using GTranslate;
using MicaTranslate.Core.Enums;
using MicaTranslate.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MicaTranslate.Services.Translators;

public class GeneralTranslatorService : ITranslationService
{
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _sp;
    private ITranslationService _current;

    public GeneralTranslatorService(IServiceProvider sp, ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _sp = sp;

        if (_current == null)
            UpdateImplementation();
    }

    public void UpdateImplementation()
    {
        _current = _settingsService.Settings.SelectedEngine switch
        {
            TranslationEngineEnum.Google => _sp.GetRequiredService<GoogleTranslationService>(),
            TranslationEngineEnum.Bing => _sp.GetRequiredService<BingTranslationService>(),
            TranslationEngineEnum.MicrosoftAzure => _sp.GetRequiredService<MicosoftAzureTranslationService>(),
            TranslationEngineEnum.Yandex => _sp.GetRequiredService<YandexTranslationService>(),
            _ => _sp.GetRequiredService<YandexTranslationService>(),
        };
    }

    public List<Language> GetLanguages() =>
        _current.GetLanguages();

    public Task<string> TranslateAsync(string text, string to, string? from, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(from))
            from = null;

        return _current.TranslateAsync(text, to, from, cancellationToken);
    }

    public async Task<Language> DetectLanguage(string text, CancellationToken cancellationToken)
    {
        return await _current.DetectLanguage(text, cancellationToken);
    }
}