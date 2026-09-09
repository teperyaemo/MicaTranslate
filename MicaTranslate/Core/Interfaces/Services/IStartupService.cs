using System.Threading.Tasks;

namespace MicaTranslate.Core.Interfaces.Services;

public interface IStartupService
{
    Task<bool> IsEnabledAsync();

    Task<bool> SetEnabledAsync(bool enabled);
}
