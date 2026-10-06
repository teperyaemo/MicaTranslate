using Microsoft.UI.Xaml.Controls;

namespace MicaTranslate.Core.Interfaces.Services;

public interface INavigationService
{
    void Initialize(Frame frame);

    bool Navigate<TPage>(object? parameter = null) where TPage : Page;

    bool CanGoBack { get; }

    void GoBack();
}