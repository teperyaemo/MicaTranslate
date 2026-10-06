using CommunityToolkit.Mvvm.ComponentModel;
using MicaTranslate.Core.Enums;
using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MicaTranslate.ViewModels;

[DynamicallyAccessedMembers(
DynamicallyAccessedMemberTypes.PublicProperties)]
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ITranslationService _translationService;
    private readonly IHotkeyService _hotkeySevice;

    public AppSettings Settings { get; }

    public SettingsViewModel(ISettingsService settingsService, ITranslationService translationService, IHotkeyService hotkeySevice)
    {
        _settingsService = settingsService;
        _translationService = translationService;
        _hotkeySevice = hotkeySevice;

        AvailableLanguages = new(_translationService.GetLanguages().Select(l => l.Name));
        Settings = _settingsService.Settings;
    }

    public ObservableCollection<TranslationEngineEnum> AvailableEngines { get; }
        = new(Enum.GetValues<TranslationEngineEnum>());

    public ObservableCollection<string> AvailableLanguages { get; }

    public ObservableCollection<string> AvailableAppLanguages { get; } =
        new() { "Русский", "English" };

    public ObservableCollection<AppTheme> AvailableThemes { get; }
        = new(Enum.GetValues<AppTheme>());

    public ObservableCollection<BackgroundEffect> BackgroundEffects { get; }
        = new(Enum.GetValues<BackgroundEffect>());

    public Func<HotkeyModel, bool> HotkeyAvailabilityChecker
        => _hotkeySevice.IsAvailable;
}