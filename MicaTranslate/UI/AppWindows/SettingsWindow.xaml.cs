using MicaTranslate.Core.Enums;
using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.System;
using WinRT.Interop;

namespace MicaTranslate.UI.AppWindows;

public sealed partial class SettingsWindow : Window, IAppearanceWindow
{
    private const uint WM_SETICON = 0x0080;

    private const nint ICON_SMALL = 0;
    private const nint ICON_BIG = 1;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;

    private nint _hIcon;

    private readonly IThemeService _themeService;

    private SettingsViewModel ViewModel =>
        (SettingsViewModel)settingsGrid.DataContext;

    public SettingsWindow()
    {
        InitializeComponent();

        SetAppVersion();

        _hIcon = SetWindowIcon(this);

        settingsGrid.DataContext =
            App.Host.Services.GetRequiredService<SettingsViewModel>();

        _themeService =
            App.Host.Services.GetRequiredService<IThemeService>();

        HotkeyControl.HotkeyAvailabilityChecker =
            ViewModel.HotkeyAvailabilityChecker;

        ExtendsContentIntoTitleBar = true;

        ConfigureTitleBar();

        _themeService.RegisterWindow(this);

        Closed += OnClosed;

        PositionCentered();
    }

    private void SetAppVersion()
    {
        var version = Package.Current.Id.Version;

        VersionTextBlock.Text =
            $"{VersionTextBlock.Text} {version.Major}.{version.Minor}.{version.Build}";
    }

    private void ConfigureTitleBar()
    {
        var hwnd =
            WindowNative.GetWindowHandle(this);

        var windowId =
            Win32Interop.GetWindowIdFromWindow(hwnd);

        var appWindow =
            AppWindow.GetFromWindowId(windowId);

        if (appWindow == null)
            return;

        var titleBar = appWindow.TitleBar;

        titleBar.PreferredHeightOption =
            TitleBarHeightOption.Tall;

        titleBar.ButtonBackgroundColor =
            Colors.Transparent;

        titleBar.ButtonInactiveBackgroundColor =
            Colors.Transparent;
    }

    private static nint SetWindowIcon(Window window)
    {
        var hwnd =
            WindowNative.GetWindowHandle(window);

        var iconPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "AppLogo32.ico");

        if (!File.Exists(iconPath))
            return nint.Zero;

        var hIcon =
            LoadImage(
                nint.Zero,
                iconPath,
                IMAGE_ICON,
                32,
                32,
                LR_LOADFROMFILE);

        if (hIcon == nint.Zero)
            return nint.Zero;

        SendMessage(
            hwnd,
            WM_SETICON,
            ICON_BIG,
            hIcon);

        SendMessage(
            hwnd,
            WM_SETICON,
            ICON_SMALL,
            hIcon);

        return hIcon;
    }

    private void PositionCentered()
    {
        var displayArea =
            DisplayArea.GetFromWindowId(
                AppWindow.Id,
                DisplayAreaFallback.Nearest);

        var workArea =
            displayArea.WorkArea;

        var size =
            AppWindow.Size;

        var x =
            workArea.X +
            (workArea.Width - size.Width) / 2;

        var y =
            workArea.Y +
            (workArea.Height - size.Height) / 2;

        AppWindow.Move(
            new Windows.Graphics.PointInt32(
                x,
                y));
    }

    public void ApplyTheme(AppTheme theme)
    {
        if (Content is not FrameworkElement root)
            return;

        root.RequestedTheme =
            theme switch
            {
                AppTheme.Light =>
                    ElementTheme.Light,

                AppTheme.Dark =>
                    ElementTheme.Dark,

                _ =>
                    ElementTheme.Default
            };
    }

    public void ApplyBackdrop(BackgroundEffect effect)
    {
        switch (effect)
        {
            case BackgroundEffect.None:
                SystemBackdrop = null;

                settingsGrid.Background =
                    (Brush)Application.Current.Resources[
                        "ApplicationPageBackgroundThemeBrush"];

                break;

            case BackgroundEffect.Mica:
            default:
                SystemBackdrop =
                    new MicaBackdrop
                    {
                        Kind = MicaKind.Base
                    };

                settingsGrid.Background =
                    new SolidColorBrush(
                        Colors.Transparent);

                break;
        }
    }

    private async void PrivacyCard_Click(
        object sender,
        RoutedEventArgs e)
    {
        await OpenUrlAsync("https://google.com");
    }

    private async void ThirdPartyNoticesCard_Click(
        object sender,
        RoutedEventArgs e)
    {
        await OpenUrlAsync("https://google.com");
    }

    private async void LicenseCard_Click(
        object sender,
        RoutedEventArgs e)
    {
        await OpenUrlAsync("https://google.com");
    }

    private async void GithubCard_Click(
        object sender,
        RoutedEventArgs e)
    {
        await OpenUrlAsync("https://google.com");
    }

    private static async System.Threading.Tasks.Task OpenUrlAsync(
        string url)
    {
        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out var uri))
        {
            return;
        }

        await Launcher.LaunchUriAsync(uri);
    }

    private void OnClosed(
        object sender,
        WindowEventArgs args)
    {
        if (_hIcon != nint.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = nint.Zero;
        }
    }

    [DllImport(
        "user32.dll",
        EntryPoint = "LoadImageW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern nint LoadImage(
        nint hInst,
        string name,
        uint type,
        int cx,
        int cy,
        uint fuLoad);

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern nint SendMessage(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam);

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern bool DestroyIcon(
        nint hIcon);
}