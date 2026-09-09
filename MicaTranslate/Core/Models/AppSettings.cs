using CommunityToolkit.Mvvm.ComponentModel;
using MicaTranslate.Core.Enums;
using System.Collections.ObjectModel;

namespace MicaTranslate.Core.Models;

public partial class AppSettings : ObservableObject
{
    // Translation
    [ObservableProperty]
    private TranslationEngineEnum selectedEngine = TranslationEngineEnum.Yandex;

    [ObservableProperty]
    private string sourceLanguage = "English";

    [ObservableProperty]
    private string targetLanguage = "German";

    [ObservableProperty]
    private string appLanguage = "English";

    [ObservableProperty]
    private AppTheme selectedTheme = AppTheme.System;

    [ObservableProperty]
    private BackgroundEffect selectedBackgroundEffect = BackgroundEffect.Acrylic;

    // Window
    [ObservableProperty]
    private double defaultWidth = 845;

    [ObservableProperty]
    private double defaultHeight = 360;

    [ObservableProperty]
    private bool startMinimized;

    [ObservableProperty]
    private bool alwaysOnTop = false;

    [ObservableProperty]
    private bool rememberWindowSize = true; //TODO: Implement it later

    [ObservableProperty]
    private bool centerOnOpen = true;

    // Advanced
    [ObservableProperty]
    private bool runAtStartup = false;

    [ObservableProperty]
    private bool keepTextOnMinimize = false;

    [ObservableProperty]
    private bool autoDetectLanguage = true;

    [ObservableProperty]
    private bool copyToClipboard;

    [ObservableProperty]
    private bool hotkeyEnabled = true;

    [ObservableProperty]
    private HotkeyModel hotkey = new()
    {
        Keys = [Windows.System.VirtualKey.LeftControl, Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.R]
    };

    [ObservableProperty]
    public ObservableCollection<string> favoriteLanguages = [];
}
