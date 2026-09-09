using CommunityToolkit.Mvvm.Messaging;
using MicaTranslate.Core.Enums;
using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Core.Messages;
using MicaTranslate.UI.AppWindows;
using MicaTranslate.UI.Pages;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using System.Threading.Tasks;
using Windows.System;

namespace MicaTranslate;

public sealed partial class MainWindow : Window, IAppearanceWindow
{
    private bool _windowInitialized;
    private bool _startHidden;

    private readonly IWindowService _windowService;
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IThemeService _themeService;
    private readonly ITrayService _trayService;
    private readonly ShellPage _shellPage;

    public MainWindow(
        IWindowService windowService,
        ISettingsService settingsService,
        IHotkeyService hotkeyService,
        IThemeService themeService,
        ITrayService trayService,
        ShellPage shellPage)
    {
        InitializeComponent();

        _windowService = windowService;
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _themeService = themeService;
        _trayService = trayService;
        _shellPage = shellPage;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(_shellPage.DragGrid);

        Content = _shellPage;

        _themeService.RegisterWindow(this);

        _hotkeyService.Initialize(this);
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        Activated += OnWindowActivated;
        _shellPage.KeyDown += MainWindow_KeyDown;

        _trayService.OpenSettingsAction = NavigateToSettings;
        _trayService.ExitAction = ExitApplication;
        _trayService.LeftClickAction = FocusTranslatorInput;
        _trayService.IconToolTip = "Mica Translate";
        _trayService.CenterWindowOnOpen =
            _settingsService.Settings.CenterOnOpen;
    }

    public void HandleLaunchNonUI(
        AppActivationArguments args)
    {
        if (args.Kind != ExtendedActivationKind.StartupTask &&
            !_settingsService.Settings.StartMinimized)
        {
            _startHidden = false;
        }
        else
        {
            _startHidden = true;
        }

        Activate();
    }

    public async Task ShowFromTray()
    {
        _trayService.ShowWindow();
        _windowService.SetAlwaysOnTop(
            _settingsService.Settings.AlwaysOnTop);

        FocusTranslatorInput();

        await Task.CompletedTask;
    }

    private void FocusTranslatorInput()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (Content is FrameworkElement root &&
                root.FindName("InputTextBox") is TextBox textBox)
            {
                textBox.Focus(FocusState.Programmatic);
                textBox.SelectAll();
            }
        });
    }

    private void OnHotkeyPressed()
    {
        if (!_settingsService.Settings.HotkeyEnabled)
            return;

        DispatcherQueue.TryEnqueue(
            async () => await ShowFromTray());
    }

    private void OnWindowActivated(
        object sender,
        WindowActivatedEventArgs e)
    {
        if (!_windowInitialized)
        {
            _windowInitialized = true;

            DispatcherQueue.TryEnqueue(() =>
            {
                _windowService.DisableTitleBarDoubleClick();

                if (_startHidden)
                {
                    _trayService.HideWindow();
                }
            });
        }

        if (_windowService.IsAlwaysOnTop())
            return;

        if (e.WindowActivationState ==
            WindowActivationState.Deactivated)
        {
            _trayService.HideWindow();
            return;
        }

        if (e.WindowActivationState ==
            WindowActivationState.CodeActivated)
        {
            WeakReferenceMessenger.Default.Send(
                new MainWindowActivatedMessage());
        }
    }

    private void MainWindow_KeyDown(
        object? sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            _trayService.HideWindow();
        }
    }

    public void NavigateToSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Activate();
    }

    private void ExitApplication()
    {
        _trayService.Dispose();
        Application.Current.Exit();
    }

    public void ApplyTheme(AppTheme theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppTheme.Light =>
                    ElementTheme.Light,

                AppTheme.Dark =>
                    ElementTheme.Dark,

                _ =>
                    ElementTheme.Default
            };
        }
    }

    public void ApplyBackdrop(
        BackgroundEffect effect)
    {
        switch (effect)
        {
            case BackgroundEffect.Mica:

                SystemBackdrop = new MicaBackdrop
                {
                    Kind = MicaKind.Base
                };

                _shellPage.RootGrid.Background =
                    new SolidColorBrush(Colors.Transparent);

                break;

            case BackgroundEffect.MicaAlt:

                SystemBackdrop = new MicaBackdrop
                {
                    Kind = MicaKind.BaseAlt
                };

                _shellPage.RootGrid.Background =
                    new SolidColorBrush(Colors.Transparent);

                break;

            case BackgroundEffect.Acrylic:

                SystemBackdrop =
                    new DesktopAcrylicBackdrop();

                _shellPage.RootGrid.Background =
                    new SolidColorBrush(Colors.Transparent);

                break;

            case BackgroundEffect.None:

                SystemBackdrop = null;

                _shellPage.RootGrid.Background =
                    (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];

                break;
        }
    }
}