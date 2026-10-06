using GTranslate;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MicaTranslate.Core.Interfaces.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string to, string? from, CancellationToken cancellationToken);

    List<Language> GetLanguages();

    void UpdateImplementation();

    Task<Language> DetectLanguage(string text, CancellationToken cancellationToken);
}