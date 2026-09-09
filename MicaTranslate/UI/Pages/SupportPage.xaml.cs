using CommunityToolkit.Mvvm.Messaging;
using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Messages;
using MicaTranslate.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.System;

namespace MicaTranslate.UI.Pages;

public sealed partial class SupportPage : Page
{
    private readonly SupportViewModel _viewModel;

    public SupportPage()
    {
        InitializeComponent();

        _viewModel = App.Host.Services.GetRequiredService<SupportViewModel>();
        DataContext = _viewModel;

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
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

    private async void GithubSettingsCard_Click(
        object sender,
        RoutedEventArgs e)
    {
        string url = "https://google.com";

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
        {
            await Launcher.LaunchUriAsync(uriResult);
        }
    }

    private async void BoostySettingsCard_Click(
        object sender,
        RoutedEventArgs e)
    {
        string url = "https://boosty.to/micatranslate";

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
        {
            await Launcher.LaunchUriAsync(uriResult);
        }
    }

    private async void CloudpaymentsSettingsCard_Click(
        object sender,
        RoutedEventArgs e)
    {
        string url = "https://pay.cloudtips.ru/p/895e9954";

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
        {
            await Launcher.LaunchUriAsync(uriResult);
        }
    }
}