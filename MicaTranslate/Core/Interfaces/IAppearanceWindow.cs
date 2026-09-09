using MicaTranslate.Core.Enums;

namespace MicaTranslate.Core.Interfaces;

public interface IAppearanceWindow
{
    void ApplyTheme(AppTheme theme);

    void ApplyBackdrop(BackgroundEffect effect);
}
