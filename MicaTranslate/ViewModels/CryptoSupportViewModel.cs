using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Interfaces.Services;

namespace MicaTranslate.ViewModels;

public partial class CryptoSupportViewModel : ObservableObject, INavigationAware
{
    private readonly IWindowService _windowService;
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;

    public CryptoSupportViewModel(ISettingsService settingsService, IWindowService windowService, INavigationService navigationService)
    {
        _windowService = windowService;
        _settingsService = settingsService;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool alwaysOnTop;

    private partial void OnAlwaysOnTopChanged(bool value)
    {
        _windowService.SetAlwaysOnTop(value);
    }

    [ObservableProperty]
    private string cryptoWalletAddress =
        "TAMXr3MCsCenDYqyA7dtXYP7ojorckxP8a";

    public void OnNavigatedTo(object? parameter)
    {
        AlwaysOnTop = _windowService.IsAlwaysOnTop();
    }

    public void SetIsAlwaysOnTop()
    {
        AlwaysOnTop = _settingsService.Settings.AlwaysOnTop;
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
    }
}