using MicaTranslate.Core.Models;

namespace MicaTranslate.Core.Interfaces.Services;

public interface IThemeService
{
    void RegisterWindow(IAppearanceWindow window);

    void Apply(AppSettings settings);
}
