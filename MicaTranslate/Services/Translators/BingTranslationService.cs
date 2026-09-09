using GTranslate;
using GTranslate.Translators;
using MicaTranslate.Core.Interfaces.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MicaTranslate.Services.Translators;

public class BingTranslationService : ITranslationService
{
    private readonly BingTranslator _translator = new();

    public async Task<string> TranslateAsync(string text, string to, string? from, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (cancellationToken.IsCancellationRequested)
            return string.Empty;

        var result = await _translator.TranslateAsync(text, to, from);
        return result.Translation;
    }

    public List<Language> GetLanguages()
    {
        return Language.LanguageDictionary
            .Values
            .Where(lang => _translator.IsLanguageSupported(lang))
            .OrderBy(lang => lang.Name)
            .ToList();
    }

    public void UpdateImplementation()
    {
        throw new System.NotImplementedException();
    }

    public async Task<Language> DetectLanguage(string text, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return default;

        return await _translator.DetectLanguageAsync(text);
    }
}
