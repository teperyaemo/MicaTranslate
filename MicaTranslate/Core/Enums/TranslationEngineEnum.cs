using System.ComponentModel.DataAnnotations;

namespace MicaTranslate.Core.Enums;

public enum TranslationEngineEnum
{
    [Display(Name = "Google Translate")] Google,
    [Display(Name = "Bing Translator")] Bing,
    [Display(Name = "Microsoft Azure Translator")] MicrosoftAzure,
    [Display(Name = "Yandex.Translate")] Yandex
}