using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace MicaTranslate.UI.AppWindows;

public sealed partial class TrayMenuWindow : Window
{
    // ---------------------------------------------------------------------
    // Window styles
    // ---------------------------------------------------------------------

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    private const long WS_POPUP = 0x80000000L;

    private const long WS_EX_TOOLWINDOW = 0x00000080L;
    private const long WS_EX_TOPMOST = 0x00000008L;
    private const long WS_EX_APPWINDOW = 0x00040000L;
    private const long WS_EX_LAYERED = 0x00080000L;

    // ---------------------------------------------------------------------
    // ShowWindow
    // ---------------------------------------------------------------------

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    // ---------------------------------------------------------------------
    // DWM
    // ---------------------------------------------------------------------

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const uint DWMWCP_ROUND = 2;
    private readonly AppWindow _appWindow;

    private readonly nint _hwnd;

    private bool _isClosing;

    private bool _isDarkTheme;

    public event Action? SettingsClicked;

    public event Action? ExitClicked;

    public TrayMenuWindow()
    {
        InitializeComponent();

        _hwnd =
            WindowNative.GetWindowHandle(this);

        var windowId =
            Win32Interop.GetWindowIdFromWindow(
                _hwnd);

        _appWindow =
            AppWindow.GetFromWindowId(
                windowId);

        if (_appWindow == null)
        {
            throw new InvalidOperationException(
                "Unable to obtain AppWindow for tray menu.");
        }

        ConfigureWindow();

        Activated +=
            OnActivated;

        Closed +=
            OnClosed;

        UpdateTheme();
    }

    // =====================================================================
    // Window configuration
    // =====================================================================

    private void ConfigureWindow()
    {
        SetWindowStyle(
            _hwnd,
            WS_POPUP);

        var exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE);

        var newExStyle =
            (exStyle.ToInt64() & ~WS_EX_APPWINDOW) |
            WS_EX_TOOLWINDOW |
            WS_EX_TOPMOST |
            WS_EX_LAYERED;

        SetWindowLongPtr(
            _hwnd,
            GWL_EXSTYLE,
            (nint)newExStyle);

        _appWindow.Resize(
            new SizeInt32(
                164,
                76));

        if (_appWindow.Presenter
            is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;

            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;

            presenter.SetBorderAndTitleBar(
                false,
                false);
        }

        SetRoundCorners();

        HideMenu();
    }

    // =====================================================================
    // Show
    // =====================================================================

    public void ShowAt(
        int x,
        int y)
    {
        if (_isClosing)
            return;

        var displayArea =
            GetDisplayAreaFromPoint(x, y);

        var workArea =
            displayArea.WorkArea;

        var menuSize = _appWindow.Size;

        int menuX = x;
        int menuY = y;

        if (menuX + menuSize.Width >
            workArea.X + workArea.Width)
        {
            menuX =
                x - menuSize.Width;
        }

        if (menuY + menuSize.Height >
            workArea.Y + workArea.Height)
        {
            menuY =
                y - menuSize.Height;
        }
        if (menuX < workArea.X)
            menuX = workArea.X;

        if (menuY < workArea.Y)
            menuY = workArea.Y;

        _appWindow.Move(
            new PointInt32(
                menuX,
                menuY));

        UpdateTheme();

        SetForegroundWindow(
            _hwnd);

        ShowWindow(
            _hwnd,
            SW_SHOW);

        IsVisible = true;
    }

    // =====================================================================
    // Hide
    // =====================================================================

    public void HideMenu()
    {
        if (_isClosing)
            return;

        ShowWindow(_hwnd, SW_HIDE);

        IsVisible = false;
    }

    // =====================================================================
    // Activation
    // =====================================================================

    private void OnActivated(
        object sender,
        WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState ==
            WindowActivationState.Deactivated)
        {
            HideMenu();
        }
    }

    // =====================================================================
    // Buttons
    // =====================================================================

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        HideMenu();

        SettingsClicked?.Invoke();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        HideMenu();

        ExitClicked?.Invoke();
    }

    // =====================================================================
    // Theme
    // =====================================================================

    private void UpdateTheme()
    {
        try
        {
            bool dark =
                ShouldSystemUseDarkMode();

            _isDarkTheme =
                dark;

            Root.RequestedTheme =
                dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light;

            SetImmersiveDarkMode(
                _hwnd,
                dark);

            SetRoundCorners();
        }
        catch
        {
            // ignored
        }
    }

    private static bool ShouldSystemUseDarkMode()
    {
        return ShouldSystemUseDarkModeNative();
    }

    private void SetImmersiveDarkMode(nint hwnd, bool dark)
    {
        int value =
            dark ? 1 : 0;

        DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref value,
            sizeof(int));
    }

    private void SetRoundCorners()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        uint preference =
            DWMWCP_ROUND;

        DwmSetWindowAttribute(
            _hwnd,
            DWMWA_WINDOW_CORNER_PREFERENCE,
            ref preference,
            sizeof(uint));
    }

    // =====================================================================
    // Display
    // =====================================================================

    private static DisplayArea GetDisplayAreaFromPoint(int x, int y)
    {
        return DisplayArea.GetFromPoint(
            new PointInt32(x, y),
            DisplayAreaFallback.Nearest);
    }

    // =====================================================================
    // Closed
    // =====================================================================

    private void OnClosed(object? sender, WindowEventArgs args)
    {
        _isClosing = true;
        IsVisible = false;
    }

    // =====================================================================
    // Win32 helpers
    // =====================================================================

    private static void SetWindowStyle(nint hwnd, long style)
    {
        SetWindowLongPtr(
            hwnd,
            GWL_STYLE,
            (nint)style);
    }

    private static nint GetWindowLongPtr(nint hwnd, int index)
    {
        if (nint.Size == 8)
        {
            return GetWindowLongPtr64(
                hwnd,
                index);
        }

        return new nint(
            GetWindowLong32(
                hwnd,
                index));
    }

    private static nint SetWindowLongPtr(
        nint hwnd,
        int index,
        nint value)
    {
        if (nint.Size == 8)
        {
            return SetWindowLongPtr64(
                hwnd,
                index,
                value);
        }

        return new nint(
            SetWindowLong32(
                hwnd,
                index,
                value.ToInt32()));
    }

    // =====================================================================
    // Native
    // =====================================================================

    [DllImport(
        "user32.dll",
        EntryPoint = "GetWindowLongPtrW",
        SetLastError = true)]
    private static extern nint GetWindowLongPtr64(
        nint hWnd,
        int nIndex);

    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLongPtrW",
        SetLastError = true)]
    private static extern nint SetWindowLongPtr64(
        nint hWnd,
        int nIndex,
        nint dwNewLong);

    [DllImport(
        "user32.dll",
        EntryPoint = "GetWindowLongW",
        SetLastError = true)]
    private static extern int GetWindowLong32(
        nint hWnd,
        int nIndex);

    [DllImport(
        "user32.dll",
        EntryPoint = "SetWindowLongW",
        SetLastError = true)]
    private static extern int SetWindowLong32(
        nint hWnd,
        int nIndex,
        int dwNewLong);

    [DllImport(
        "user32.dll")]
    private static extern bool SetForegroundWindow(
        nint hWnd);

    [DllImport(
        "user32.dll")]
    private static extern bool ShowWindow(
        nint hWnd,
        int nCmdShow);

    [DllImport(
        "user32.dll")]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [DllImport(
        "dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    [DllImport(
        "dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int dwAttribute,
        ref uint pvAttribute,
        int cbAttribute);

    [DllImport(
        "UxTheme.dll",
        EntryPoint = "#138",
        SetLastError = true)]
    private static extern bool ShouldSystemUseDarkModeNative();

    // =====================================================================
    // State
    // =====================================================================

    public bool IsVisible
    {
        get;
        private set;
    }
}