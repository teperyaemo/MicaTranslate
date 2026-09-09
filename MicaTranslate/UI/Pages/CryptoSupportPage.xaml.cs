using CommunityToolkit.Mvvm.Messaging;
using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Messages;
using MicaTranslate.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace MicaTranslate.UI.Pages;

public sealed partial class CryptoSupportPage : Page
{
    private readonly CryptoSupportViewModel _viewModel;

    private readonly ResourceLoader _resourceLoader;

    public CryptoSupportPage()
    {
        InitializeComponent();

        _viewModel = App.Host.Services.GetRequiredService<CryptoSupportViewModel>();
        _resourceLoader = new ResourceLoader();

        DataContext = _viewModel;

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        WeakReferenceMessenger.Default.Register<MainWindowActivatedMessage>(
            this,
            OnMainWindowActivated);

        if (DataContext is INavigationAware vm)
        {
            vm.OnNavigatedTo(e.Parameter);
        }
    }

    private void OnMainWindowActivated(
        object sender,
        MainWindowActivatedMessage message)
    {
        _viewModel.SetIsAlwaysOnTop();
    }

    private async void CopyWalletButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string walletAddress = _viewModel.CryptoWalletAddress;

        if (string.IsNullOrWhiteSpace(walletAddress))
            return;

        DataPackage package = new();
        package.SetText(walletAddress);

        Clipboard.SetContent(package);

        CopyWalletButton.Content = _resourceLoader.GetString(
            "CryptoSupportPage_CopyWalletBtn_Copied");

        await Task.Delay(1500);

        CopyWalletButton.Content = _resourceLoader.GetString(
            "CryptoSupportPage_CopyWalletBtn_Content");
    }
}