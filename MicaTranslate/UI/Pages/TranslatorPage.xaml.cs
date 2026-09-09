using CommunityToolkit.Mvvm.Messaging;
using MicaTranslate.Core.Interfaces;
using MicaTranslate.Core.Messages;
using MicaTranslate.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MicaTranslate.UI.Pages;

public sealed partial class TranslatorPage : Page
{
    private readonly TranslatorViewModel _viewModel;

    public TranslatorPage()
    {
        InitializeComponent();

        _viewModel = App.Host.Services.GetRequiredService<TranslatorViewModel>();
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

        WeakReferenceMessenger.Default.Register<MainWindowActivatedMessage>(this, OnMainWindowActivated);

        if (DataContext is INavigationAware vm)
        {
            vm.OnNavigatedTo(e.Parameter);
        }
    }

    private void OnMainWindowActivated(object sender, MainWindowActivatedMessage message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            InputTextBox.Focus(FocusState.Programmatic);
        });

        _viewModel.ClearTextIfNeeded();
        _viewModel.SetIsAlwaysOnTop();
        _viewModel.SetAutoDetectLanguage();
        _viewModel.SetDefaultLanguages();
    }

    private void LanguagesComboBox_DropDownClosed(object sender, object e)
    {
        _viewModel.SortLanguagesCommand.Execute(null);
    }
}
