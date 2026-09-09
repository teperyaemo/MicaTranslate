using MicaTranslate.Core.Models;
using System.Threading.Tasks;

namespace MicaTranslate.Core.Interfaces.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }

    Task LoadAsync();
    Task SaveAsync();
}
