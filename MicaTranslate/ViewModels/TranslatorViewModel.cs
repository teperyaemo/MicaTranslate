using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Core.Models;
using MicaTranslate.UI.AppWindows;
using MicaTranslate.UI.Pages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MicaTranslate.ViewModels;

public partial class TranslatorViewModel : ObservableObject, INavigationAware
{
    private readonly ITranslationService _translationService;
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;
    private readonly ISettingsService _settingsService;
    private readonly IClipboardService _clipboardService;

    private CancellationTokenSource? _debounceCts;

    public TranslatorViewModel(
        ITranslationService translationService,
        INavigationService navigationService,
        IWindowService windowService,
        ISettingsService settingsService,
        IClipboardService clipboardService)
    {
        _translationService = translationService;
        _navigationService = navigationService;
        _windowService = windowService;
        _settingsService = settingsService;
        _clipboardService = clipboardService;

        LoadLanguages();
    }

    public ObservableCollection<LanguageModel> SourceLanguages { get; } = new();
    public ObservableCollection<LanguageModel> TargetLanguages { get; } = new();

    // ===== Source Text =====
    [ObservableProperty]
    private string sourceText;
    partial void OnSourceTextChanged(string value)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        var token = _debounceCts.Token;

        _ = DebounceTranslateAsync(token);
    }

    // ===== Translated Text =====
    [ObservableProperty]
    private string translatedText;
    partial void OnTranslatedTextChanged(string value)
    {
        if (_settingsService.Settings.CopyToClipboard &&
            !string.IsNullOrWhiteSpace(value))
        {
            _clipboardService.SetText(value);
        }
    }

    // ===== Languages =====
    [ObservableProperty]
    private LanguageModel selectedSourceLanguage;

    partial void OnSelectedSourceLanguageChanged(LanguageModel? value)
    {
        OnSourceTextChanged(SourceText);
    }

    [ObservableProperty]
    private LanguageModel selectedTargetLanguage;

    partial void OnSelectedTargetLanguageChanged(LanguageModel? value)
    {
        OnSourceTextChanged(SourceText);
    }

    [ObservableProperty]
    private bool alwaysOnTop;

    partial void OnAlwaysOnTopChanged(bool value)
    {
        _windowService.SetAlwaysOnTop(value);
    }

    [ObservableProperty]
    private bool autoDetectLanguage;

    [ObservableProperty]
    private string detectedLanguage = "";

    private async Task DebounceTranslateAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(300, token);

            if (!token.IsCancellationRequested)
            {
                await TranslateAsync(token);
            }
        }
        catch (TaskCanceledException)
        {
            //ignored
        }
    }

    public void LoadLanguages()
    {
        var favLangs = _settingsService.Settings.FavoriteLanguages;
        var langs = _translationService
            .GetLanguages()
            .OrderByDescending(lang => favLangs.Contains(lang.ISO6391))
            .ThenBy(l => l.Name);

        TargetLanguages.Clear();
        SourceLanguages.Clear();
        SourceLanguages.Add(new LanguageModel
        {
            Name = "Auto",
            TargetLanguage = null,
            CanFavorite = false,
        });

        foreach (var lang in langs)
        {
            var langModel = new LanguageModel
            {
                Name = lang.Name,
                TargetLanguage = lang,
                IsFavorite = favLangs.Contains(lang.ISO6391),
            };

            SourceLanguages.Add(langModel);
            TargetLanguages.Add(langModel);
        }

        SetDefaultLanguages();
    }

    public void ClearTextIfNeeded()
    {
        if (!_settingsService.Settings.KeepTextOnMinimize)
        {
            SourceText = string.Empty;
            TranslatedText = string.Empty;
        }
    }

    public void SetIsAlwaysOnTop()
    {
        AlwaysOnTop = _settingsService.Settings.AlwaysOnTop;
    }

    public void SetAutoDetectLanguage()
    {
        AutoDetectLanguage = _settingsService.Settings.AutoDetectLanguage;
    }

    public void SetDefaultLanguages()
    {
        var settings = _settingsService.Settings;

        if (settings.KeepTextOnMinimize && !string.IsNullOrWhiteSpace(SourceText))
            return;

        if (settings.AutoDetectLanguage)
        {
            SelectedSourceLanguage = SourceLanguages[0];
        }
        else
        {
            SelectedSourceLanguage = SourceLanguages
                .FirstOrDefault(l => l.Name == settings.SourceLanguage)!;
        }

        SelectedTargetLanguage = TargetLanguages
            .FirstOrDefault(l => l.Name == settings.TargetLanguage)!;
    }

    private void RearrangeLanguages(ObservableCollection<LanguageModel> languages)
    {
        var selected = ReferenceEquals(languages, SourceLanguages)
            ? SelectedSourceLanguage
            : SelectedTargetLanguage;

        var sorted = languages
            .OrderBy(l => l.TargetLanguage != null)
            .ThenByDescending(l => l.IsFavorite)
            .ThenBy(l => l.Name)
            .ToList();

        languages.Clear();

        foreach (var item in sorted)
            languages.Add(item);

        if (ReferenceEquals(languages, SourceLanguages))
            SelectedSourceLanguage = selected;
        else
            SelectedTargetLanguage = selected;
    }

    // ===== Commands =====

    [RelayCommand]
    private async Task TranslateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SourceText))
        {
            DetectedLanguage = string.Empty;
            TranslatedText = string.Empty;
            return;
        }

        var settings = _settingsService.Settings;
        bool isAutoDetectMode = SelectedSourceLanguage.TargetLanguage is null;

        if (isAutoDetectMode)
        {
            var detectedLang = await _translationService.DetectLanguage(SourceText, cancellationToken);
            DetectedLanguage = $"Detected: {detectedLang.Name}";

            if (detectedLang.Name == SelectedTargetLanguage.Name)
            {
                SelectedTargetLanguage = TargetLanguages.First(l => l.Name == settings.SourceLanguage);
            }
        }
        else
        {
            DetectedLanguage = string.Empty;

            if (SelectedSourceLanguage.TargetLanguage!.Name == SelectedTargetLanguage.Name)
            {
                SelectedTargetLanguage = TargetLanguages.First(l => l.Name == settings.SourceLanguage);
            }
        }

        TranslatedText = await _translationService.TranslateAsync(
            SourceText,
            SelectedTargetLanguage?.TargetLanguage?.ISO6391!,
            SelectedSourceLanguage?.TargetLanguage?.ISO6391,
            cancellationToken
        );
    }

    [RelayCommand]
    private async Task SwapLanguages()
    {
        if (string.IsNullOrWhiteSpace(SourceText) &&
            SelectedSourceLanguage.TargetLanguage == null)
            return;

        LanguageModel tempLangModel;

        if (SelectedSourceLanguage.TargetLanguage == null)
        {
            var detectedLang = await _translationService.DetectLanguage(SourceText, CancellationToken.None);
            tempLangModel = TargetLanguages.First(l => l.TargetLanguage == detectedLang);
        }
        else
            tempLangModel = SelectedSourceLanguage;

        SelectedSourceLanguage = SourceLanguages.First(l => l.Name == SelectedTargetLanguage.Name);
        SelectedTargetLanguage = tempLangModel;

        (SourceText, TranslatedText) = (TranslatedText, SourceText);
    }

    [RelayCommand]
    private void OpenSupport()
    {
        _navigationService.Navigate<SupportPage>();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        AlwaysOnTop = false;
        var settingsWindow = new SettingsWindow();
        settingsWindow.Activate();
    }

    [RelayCommand]
    private void ToggleFavorite(LanguageModel language)
    {
        var favoriteLanguages = _settingsService.Settings.FavoriteLanguages;
        var langIso = language.TargetLanguage!.ISO6391;

        if (favoriteLanguages.Contains(langIso))
        {
            favoriteLanguages.Remove(langIso);
            language.IsFavorite = false;
        }
        else
        {
            favoriteLanguages.Add(langIso);
            language.IsFavorite = true;
        }
    }

    [RelayCommand]
    private void SortLanguages()
    {
        RearrangeLanguages(SourceLanguages);
        RearrangeLanguages(TargetLanguages);
    }

    public void OnNavigatedTo(object? parameter)
    {
        AlwaysOnTop = _windowService.IsAlwaysOnTop();
    }
}

