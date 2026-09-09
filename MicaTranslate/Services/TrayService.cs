using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.UI.AppWindows;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Graphics;
using WinRT.Interop;

namespace MicaTranslate.Services;

public sealed class TrayService : ITrayService
{
    private const string WindowClassName =
        "MicaTranslate.TrayWindow";

    private const uint WM_APP = 0x8000;
    private const uint WM_TRAYICON = WM_APP + 1;
    private const uint WM_TRAY_EXIT = WM_APP + 2;

    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;

    private const uint SW_RESTORE = 9;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    private const uint WS_EX_TOOLWINDOW = 0x00000080;

    private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

    private const nint HWND_MESSAGE = -3;

    private AppWindow? _appWindow;

    private nint _mainHwnd;
    private nint _trayHwnd;
    private nint _icon;

    private Thread? _trayThread;

    private readonly ManualResetEventSlim _trayReady = new(false);

    private DispatcherQueue? _dispatcherQueue;

    private bool _initialized;
    private bool _disposed;

    private string _iconToolTip = "Mica Translate";

    private TrayMenuWindow? _menuWindow;

    private WndProcDelegate? _wndProcDelegate;

    public bool IsInitialized =>
        _initialized;

    public bool IsVisible { get; private set; }

    public bool CenterWindowOnOpen { get; set; } = true;

    public string IconToolTip
    {
        get => _iconToolTip;

        set
        {
            _iconToolTip = value ?? string.Empty;

            if (_initialized)
            {
                PostTrayMessage(WM_TRAYICON);
            }
        }
    }

    public Action? OpenSettingsAction { get; set; }

    public Action? ExitAction { get; set; }

    public Action? LeftClickAction { get; set; }

    public void Initialize(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_initialized)
            return;

        _mainHwnd = WindowNative.GetWindowHandle(window);

        _dispatcherQueue =
            DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException(
                "Unable to obtain DispatcherQueue.");

        _appWindow = GetAppWindow(window);

        LoadTrayIcon();

        _menuWindow = new TrayMenuWindow();

        _menuWindow.SettingsClicked += OnSettingsClicked;
        _menuWindow.ExitClicked += OnExitClicked;
        _menuWindow.Closed += (_, _) => _menuWindow.HideMenu();

        _trayReady.Reset();

        _trayThread = new Thread(TrayThreadMain)
        {
            IsBackground = true,
            Name = "MicaTranslate Tray Thread"
        };

        _trayThread.Start();

        if (!_trayReady.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException(
                "Tray thread failed to initialize.");
        }

        _initialized = true;
        IsVisible = true;
    }

    private static AppWindow GetAppWindow(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

        return AppWindow.GetFromWindowId(windowId)
            ?? throw new InvalidOperationException(
                "Unable to obtain AppWindow.");
    }

    private void TrayThreadMain()
    {
        try
        {
            RegisterTrayWindowClass();

            _trayHwnd =
                CreateWindowEx(
                    WS_EX_TOOLWINDOW,
                    WindowClassName,
                    "Mica Translate Tray",
                    0,
                    0,
                    0,
                    0,
                    0,
                    HWND_MESSAGE,
                    0,
                    GetModuleHandle(null),
                    nint.Zero);

            if (_trayHwnd == nint.Zero)
            {
                throw new InvalidOperationException(
                    "Unable to create tray HWND.");
            }

            AddTrayIcon();

            _trayReady.Set();

            MSG msg;

            while (GetMessage(
                       out msg,
                       nint.Zero,
                       0,
                       0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        catch
        {
            _trayReady.Set();
        }
        finally
        {
            RemoveTrayIcon();

            if (_trayHwnd != nint.Zero)
            {
                DestroyWindow(_trayHwnd);
                _trayHwnd = nint.Zero;
            }
        }
    }

    private void RegisterTrayWindowClass()
    {
        _wndProcDelegate = TrayWindowProc;

        var wndProc =
            Marshal.GetFunctionPointerForDelegate(
                _wndProcDelegate);

        var wc =
            new WNDCLASSEX
            {
                cbSize =
                    (uint)Marshal.SizeOf<WNDCLASSEX>(),

                lpfnWndProc =
                    wndProc,

                hInstance =
                    GetModuleHandle(null),

                lpszClassName =
                    WindowClassName
            };

        var atom =
            RegisterClassEx(ref wc);

        if (atom == 0)
        {
            var error =
                Marshal.GetLastWin32Error();

            if (error != ERROR_CLASS_ALREADY_EXISTS)
            {
                throw new InvalidOperationException(
                    $"RegisterClassEx failed. Error={error}");
            }
        }
    }

    private nint TrayWindowProc(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam)
    {
        if (msg == WM_TRAYICON)
        {
            var mouseMessage =
                unchecked(
                    (uint)lParam.ToInt64());

            switch (mouseMessage)
            {
                case WM_LBUTTONUP:
                    InvokeOnUiThread(ShowWindow);
                    return nint.Zero;

                case WM_RBUTTONUP:
                    InvokeOnUiThread(ShowTrayMenu);
                    return nint.Zero;
            }
        }

        if (msg == WM_TRAY_EXIT)
        {
            PostQuitMessage(0);
            return nint.Zero;
        }

        return DefWindowProc(
            hWnd,
            msg,
            wParam,
            lParam);
    }

    private void ShowTrayMenu()
    {
        if (!_initialized ||
            _menuWindow == null)
        {
            return;
        }

        if (!GetCursorPos(out var point))
            return;

        _menuWindow.ShowAt(
            point.X,
            point.Y);
    }

    private void OnSettingsClicked()
    {
        _menuWindow?.HideMenu();
        OpenSettingsAction?.Invoke();
    }

    private void OnExitClicked()
    {
        _menuWindow?.HideMenu();
        ExitAction?.Invoke();
    }

    private void InvokeOnUiThread(Action action)
    {
        var dispatcher = _dispatcherQueue;

        if (dispatcher == null)
            return;

        dispatcher.TryEnqueue(
            DispatcherQueuePriority.High,
            () =>
            {
                try
                {
                    action();
                }
                catch
                {
                }
            });
    }

    private void PostTrayMessage(uint message)
    {
        if (_trayHwnd == nint.Zero)
            return;

        PostMessage(
            _trayHwnd,
            message,
            nint.Zero,
            nint.Zero);
    }

    private void LoadTrayIcon()
    {
        var iconPath =
            Path.Combine(
                Package.Current
                    .InstalledLocation
                    .Path,
                "Assets",
                "AppLogo32.ico");

        if (!File.Exists(iconPath))
        {
            throw new FileNotFoundException(
                "Tray icon was not found.",
                iconPath);
        }

        _icon =
            LoadImage(
                nint.Zero,
                iconPath,
                IMAGE_ICON,
                0,
                0,
                LR_LOADFROMFILE |
                LR_DEFAULTSIZE);

        if (_icon == nint.Zero)
        {
            throw new InvalidOperationException(
                "Unable to load tray icon.");
        }
    }

    private void AddTrayIcon()
    {
        var data =
            CreateNotifyIconData();

        if (!Shell_NotifyIcon(
                NIM_ADD,
                ref data))
        {
            throw new InvalidOperationException(
                "Unable to add tray icon.");
        }
    }

    private void RemoveTrayIcon()
    {
        if (_trayHwnd == nint.Zero)
            return;

        var data =
            CreateNotifyIconData();

        Shell_NotifyIcon(
            NIM_DELETE,
            ref data);
    }

    private NOTIFYICONDATA CreateNotifyIconData()
    {
        var tooltip =
            _iconToolTip ?? string.Empty;

        if (tooltip.Length >= 128)
            tooltip = tooltip[..127];

        return new NOTIFYICONDATA
        {
            cbSize =
                (uint)Marshal.SizeOf<
                    NOTIFYICONDATA>(),

            hWnd =
                _trayHwnd,

            uID = 1,

            uFlags =
                NIF_MESSAGE |
                NIF_ICON |
                NIF_TIP,

            uCallbackMessage =
                WM_TRAYICON,

            hIcon =
                _icon,

            szTip =
                tooltip
        };
    }

    public void ShowWindow()
    {
        if (!_initialized ||
            _appWindow == null)
        {
            return;
        }

        _menuWindow?.HideMenu();

        if (CenterWindowOnOpen)
            CenterWindow();

        _appWindow.Show();

        ShowWindow(
            _mainHwnd,
            SW_RESTORE);

        SetForegroundWindow(
            _mainHwnd);

        IsVisible = true;

        LeftClickAction?.Invoke();
    }

    public void HideWindow()
    {
        if (!_initialized ||
            _appWindow == null)
        {
            return;
        }

        _appWindow.Hide();

        _menuWindow?.HideMenu();

        IsVisible = false;
    }

    public void ToggleWindow()
    {
        if (!_initialized ||
            _appWindow == null)
        {
            return;
        }

        if (_appWindow.IsVisible)
            HideWindow();
        else
            ShowWindow();
    }

    private void CenterWindow()
    {
        if (_appWindow == null)
            return;

        var displayArea =
            DisplayArea.GetFromWindowId(
                _appWindow.Id,
                DisplayAreaFallback.Nearest);

        var workArea =
            displayArea.WorkArea;

        var size =
            _appWindow.Size;

        var x =
            workArea.X +
            (workArea.Width - size.Width) / 2;

        var y =
            workArea.Y +
            (workArea.Height - size.Height) / 2;

        _appWindow.Move(
            new PointInt32(x, y));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        InvokeOnUiThread(
            () =>
            {
                if (_menuWindow == null)
                    return;

                _menuWindow.SettingsClicked -=
                    OnSettingsClicked;

                _menuWindow.ExitClicked -=
                    OnExitClicked;

                _menuWindow.HideMenu();
                _menuWindow.Close();

                _menuWindow = null;
            });

        if (_trayHwnd != nint.Zero)
        {
            PostMessage(
                _trayHwnd,
                WM_TRAY_EXIT,
                nint.Zero,
                nint.Zero);
        }

        if (_trayThread != null &&
            _trayThread.IsAlive &&
            Thread.CurrentThread != _trayThread)
        {
            _trayThread.Join(
                TimeSpan.FromSeconds(2));
        }

        if (_icon != nint.Zero)
        {
            DestroyIcon(_icon);
            _icon = nint.Zero;
        }

        _trayThread = null;
        _initialized = false;
        IsVisible = false;
        _wndProcDelegate = null;
    }

    ~TrayService()
    {
        Dispose();
    }

    private delegate nint WndProcDelegate(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam);

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;

        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 256)]
        public string szInfo;

        public uint uTimeoutOrVersion;

        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern ushort RegisterClassEx(
        ref WNDCLASSEX lpwcx);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern nint CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int X,
        int Y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hWndMenu,
        nint hInstance,
        nint lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(
        nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(
        out MSG lpMsg,
        nint hWnd,
        uint minFilter,
        uint maxFilter);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(
        ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessage(
        ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(
        int exitCode);

    [DllImport(
        "kernel32.dll",
        CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(
        string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(
        out POINT point);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(
        nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(
        nint hWnd,
        uint command);

    [DllImport(
        "shell32.dll",
        CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(
        uint message,
        ref NOTIFYICONDATA data);

    [DllImport(
        "user32.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern nint LoadImage(
        nint hInst,
        string name,
        uint type,
        int cx,
        int cy,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(
        nint hIcon);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(
        nint hWnd,
        uint message,
        nint wParam,
        nint lParam);
}