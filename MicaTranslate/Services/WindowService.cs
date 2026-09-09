using MicaTranslate.Core.Interfaces.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace MicaTranslate.Services;

public sealed class WindowService : IWindowService
{
    private const int GWL_WNDPROC = -4;
    private const int GWL_EXSTYLE = -20;

    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    private const uint WM_NCLBUTTONDBLCLK = 0x00A3;

    private AppWindow? _appWindow;
    private nint _hWnd;

    private nint _oldWndProc;
    private WndProcDelegate? _wndProcDelegate;

    private bool _disableTitleBarDoubleClick;
    private bool _initialized;

    public event WindowMessageReceivedHandler? WindowMessageReceived;

    public event EventHandler<AppWindowClosingEventArgs>? Closing;

    private delegate nint WndProcDelegate(
        nint hWnd,
        uint message,
        nint wParam,
        nint lParam);

    public void Initialize(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_initialized)
            return;

        _hWnd = WindowNative.GetWindowHandle(window);

        var windowId =
            Win32Interop.GetWindowIdFromWindow(_hWnd);

        _appWindow =
            AppWindow.GetFromWindowId(windowId);

        if (_appWindow == null)
        {
            throw new InvalidOperationException(
                "Unable to obtain AppWindow.");
        }

        _appWindow.Closing += OnAppWindowClosing;

        InstallWndProc();

        _initialized = true;
    }

    private AppWindow AppWindow =>
        _appWindow ?? throw new InvalidOperationException(
            "WindowService is not initialized.");

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        AppWindow.Resize(
            new SizeInt32(width, height));

        Center();
    }

    public void Center()
    {
        var displayArea =
            DisplayArea.GetFromWindowId(
                AppWindow.Id,
                DisplayAreaFallback.Nearest);

        var workArea = displayArea.WorkArea;

        var x =
            workArea.X +
            (workArea.Width - AppWindow.Size.Width) / 2;

        var y =
            workArea.Y +
            (workArea.Height - AppWindow.Size.Height) / 2;

        AppWindow.Move(
            new PointInt32(x, y));
    }

    public void Show()
    {
        AppWindow.Show();
    }

    public void Hide()
    {
        AppWindow.Hide();
    }

    public bool IsVisible()
    {
        return AppWindow.IsVisible;
    }

    public void SetAlwaysOnTop(bool value)
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = value;
        }
    }

    public bool IsAlwaysOnTop()
    {
        return AppWindow.Presenter is OverlappedPresenter presenter
               && presenter.IsAlwaysOnTop;
    }

    public void DisableResize()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }
    }

    public void RemoveCaptionButtons()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;

            presenter.SetBorderAndTitleBar(
                true,
                false);
        }
    }

    public void HideFromTaskbar()
    {
        var exStyle = GetWindowLongPtr(
            _hWnd,
            GWL_EXSTYLE);

        exStyle = (nint)(
            ((long)exStyle | WS_EX_TOOLWINDOW)
            & ~WS_EX_APPWINDOW);

        SetWindowLongPtr(
            _hWnd,
            GWL_EXSTYLE,
            exStyle);
    }

    public void DisableTitleBarDoubleClick()
    {
        _disableTitleBarDoubleClick = true;
    }

    private void InstallWndProc()
    {
        if (_wndProcDelegate != null)
            return;

        _wndProcDelegate = WindowProc;

        var newWndProc =
            Marshal.GetFunctionPointerForDelegate(
                _wndProcDelegate);

        _oldWndProc =
            SetWindowLongPtr(
                _hWnd,
                GWL_WNDPROC,
                newWndProc);

        if (_oldWndProc == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to subclass the application window.");
        }
    }

    private nint WindowProc(
        nint hWnd,
        uint message,
        nint wParam,
        nint lParam)
    {
        WindowMessageReceived?.Invoke(
            hWnd,
            message,
            wParam,
            lParam);

        if (_disableTitleBarDoubleClick &&
            message == WM_NCLBUTTONDBLCLK)
        {
            return IntPtr.Zero;
        }

        return CallWindowProc(
            _oldWndProc,
            hWnd,
            message,
            wParam,
            lParam);
    }

    private void OnAppWindowClosing(
        AppWindow sender,
        AppWindowClosingEventArgs args)
    {
        Closing?.Invoke(
            sender,
            args);
    }

    private static nint GetWindowLongPtr(
        nint hWnd,
        int index)
    {
        if (IntPtr.Size == 8)
        {
            return GetWindowLongPtr64(
                hWnd,
                index);
        }

        return new nint(
            GetWindowLong32(
                hWnd,
                index));
    }

    private static nint SetWindowLongPtr(
        nint hWnd,
        int index,
        nint value)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(
                hWnd,
                index,
                value);
        }

        return new nint(
            SetWindowLong32(
                hWnd,
                index,
                value.ToInt32()));
    }

    [DllImport(
        "user32.dll",
        EntryPoint = "GetWindowLongPtrW",
        SetLastError = true)]
    private static extern nint GetWindowLongPtr64(
        nint hWnd,
        int index);

    [DllImport(
        "user32.dll",
        EntryPoint = "GetWindowLongW",
        SetLastError = true)]
    private static extern int GetWindowLong32(
        nint hWnd,
        int index);

    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLongPtrW",
        SetLastError = true)]
    private static extern nint SetWindowLongPtr64(
        nint hWnd,
        int index,
        nint value);

    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLongW",
        SetLastError = true)]
    private static extern int SetWindowLong32(
        nint hWnd,
        int index,
        int value);

    [DllImport(
        "user32.dll",
        EntryPoint = "CallWindowProcW",
        SetLastError = true)]
    private static extern nint CallWindowProc(
        nint previousWndProc,
        nint hWnd,
        uint message,
        nint wParam,
        nint lParam);
}