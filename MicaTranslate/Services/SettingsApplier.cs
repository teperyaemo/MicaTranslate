using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Core.Models;
using Microsoft.Windows.Globalization;
using System.Threading.Tasks;

namespace MicaTranslate.Services;

public class SettingsApplier(
    ISettingsService settingsService,
    IWindowService windowService,
    IThemeService themeService,
    IHotkeyService hotkeyService,
    ITranslationService translationService,
    IStartupService startupService)
{
    public async Task ApplyAsync(string propertyName)
    {
        var settings = settingsService.Settings;

        switch (propertyName)
        {
            case nameof(AppSettings.AlwaysOnTop):
                windowService.SetAlwaysOnTop(
                    settings.AlwaysOnTop);
                break;
            case nameof(AppSettings.SelectedTheme):
            case nameof(AppSettings.SelectedBackgroundEffect):
                themeService.Apply(settings);
                break;
            case nameof(AppSettings.Hotkey):
                hotkeyService.Register(settings.Hotkey);
                break;
            case nameof(AppSettings.DefaultWidth):
            case nameof(AppSettings.DefaultHeight):
                windowService.Resize((int)settings.DefaultWidth,
                    (int)settings.DefaultHeight);
                break;
            case nameof(AppSettings.SelectedEngine):
                translationService.UpdateImplementation();
                break;
            case nameof(AppSettings.AppLanguage):
                ApplyLanguage(settings.AppLanguage);
                break;
            case nameof(AppSettings.RunAtStartup):
                await startupService.SetEnabledAsync(settings.RunAtStartup);
                break;
        }
    }

    public void ApplyLanguage(string lang)
    {
        var languageTag = lang switch
        {
            "Русский" => "ru-RU",
            "English" => "en-US",
            _ => "en-US"
        };

        ApplicationLanguages.PrimaryLanguageOverride = languageTag;
    }

    public void ApplyStartupSettings()
    {
        var settings = settingsService.Settings;

        windowService.Resize((int)settings.DefaultWidth,
                    (int)settings.DefaultHeight);
        themeService.Apply(settings);
        hotkeyService.Register(settings.Hotkey);
    }
}
