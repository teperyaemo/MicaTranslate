using MicaTranslate.Core.Interfaces.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MicaTranslate.UI.Pages;

public sealed partial class ShellPage : Page
{
    public Grid DragGrid { get => DragRegion; }
    public Grid RootGrid { get => root; }
    public Frame NavFrame { get => NavigationFrame; }
    private readonly INavigationService _navigationService;

    public ShellPage(INavigationService navigationService)
    {
        this.InitializeComponent();
        _navigationService = navigationService;
        navigationService.Initialize(NavigationFrame);

        Loaded += ShellPage_Loaded;
    }

    private void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ShellPage_Loaded;

        if (NavFrame.Content == null)
        {
            _navigationService.Navigate<TranslatorPage>();
        }
    }
}