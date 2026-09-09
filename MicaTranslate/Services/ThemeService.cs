using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Core.Models;
using System.Collections.Generic;

namespace MicaTranslate.Services;

public class ThemeService(ISettingsService settingsService) : IThemeService
{
    private readonly HashSet<IAppearanceWindow> _windows = [];

    public void RegisterWindow(IAppearanceWindow window)
    {
        _windows.Add(window);
        ApplyOnWindow(window);
    }

    public void Apply(AppSettings settings)
    {
        foreach (var window in _windows)
        {
            try
            {
                window.ApplyTheme(settings.SelectedTheme);

                window.ApplyBackdrop(
                    settings.SelectedBackgroundEffect);
            }
            catch
            {
                _windows.Remove(window);
            }
        }
    }

    private void ApplyOnWindow(IAppearanceWindow window)
    {
        var settings = settingsService.Settings;

        try
        {
            window.ApplyTheme(settings.SelectedTheme);

            window.ApplyBackdrop(
                settings.SelectedBackgroundEffect);
        }
        catch
        {
            _windows.Remove(window);
        }
    }
}
