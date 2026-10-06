using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Services;
using MicaTranslate.Services.Translators;
using MicaTranslate.UI.Pages;
using MicaTranslate.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace MicaTranslate;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }

    private static ISettingsService _settingsService = null!;
    private SettingsApplier _settingsApplier = null!;
    private CancellationTokenSource? _saveCts;

    public static IHost Host { get; }
    private static Mutex? _appMutex;

    static App()
    {
        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IHotkeyService, HotkeyService>();
                services.AddSingleton<IWindowService, WindowService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IStartupService, StartupService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IClipboardService, ClipboardService>();
                services.AddSingleton<ITrayService, TrayService>();

                services.AddSingleton<ShellPage>();

                services.AddSingleton<GoogleTranslationService>();
                services.AddSingleton<YandexTranslationService>();
                services.AddSingleton<BingTranslationService>();
                services.AddSingleton<MicosoftAzureTranslationService>();
                services.AddSingleton<ITranslationService, GeneralTranslatorService>();

                services.AddTransient<SettingsApplier>();

                services.AddTransient<CryptoSupportViewModel>();
                services.AddTransient<SupportViewModel>();
                services.AddTransient<TranslatorViewModel>();
                services.AddTransient<SettingsViewModel>();

                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    public App()
    {
        _appMutex = new Mutex(
            initiallyOwned: true,
            name: @"Global\MicaTranslateAppMutex",
            createdNew: out bool createdNew);

        if (!createdNew)
        {
            Environment.Exit(0);
            return;
        }

        InitializeComponent();

        this.UnhandledException += (s, e) =>
        {
            System.IO.File.WriteAllText("crash.log", e.Exception.ToString());
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            System.IO.File.WriteAllText("crash_domain.log", e.ExceptionObject?.ToString());
        };
    }

    protected override async void OnLaunched(
        LaunchActivatedEventArgs args)
    {
        await Host.StartAsync();

        _settingsService =
            Host.Services.GetRequiredService<ISettingsService>();

        await _settingsService.LoadAsync();

        _settingsApplier =
            Host.Services.GetRequiredService<SettingsApplier>();

        _settingsService.Settings.PropertyChanged +=
            OnSettingsChanged;

        _settingsService.Settings.FavoriteLanguages.CollectionChanged +=
            FavoriteLanguagesChanged;

        MainWindow =
            Host.Services.GetRequiredService<MainWindow>();

        var windowService =
            Host.Services.GetRequiredService<IWindowService>();

        windowService.Initialize(MainWindow);

        windowService.DisableResize();
        windowService.RemoveCaptionButtons();
        windowService.HideFromTaskbar();

        var trayService =
            Host.Services.GetRequiredService<ITrayService>();

        trayService.Initialize(MainWindow);

        trayService.CenterWindowOnOpen =
            _settingsService.Settings.CenterOnOpen;

        var activatedEventArgs =
            Microsoft.Windows.AppLifecycle.AppInstance
                .GetCurrent()
                .GetActivatedEventArgs();

        MainWindow.HandleLaunchNonUI(
            activatedEventArgs);

        _settingsApplier.ApplyStartupSettings();
    }

    private async void FavoriteLanguagesChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        await ScheduleSaveAsync();
    }

    private async void OnSettingsChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        await _settingsApplier.ApplyAsync(
            e.PropertyName);

        await ScheduleSaveAsync();
    }

    private async Task ScheduleSaveAsync()
    {
        _saveCts?.Cancel();

        _saveCts = new CancellationTokenSource();

        await SaveAsync(_saveCts.Token);
    }

    private async Task SaveAsync(
        CancellationToken token)
    {
        try
        {
            await Task.Delay(500, token);

            await _settingsService.SaveAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }
}