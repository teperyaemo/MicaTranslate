using MicaTranslate.Core.Interfaces.Services;
using Microsoft.UI.Xaml.Controls;
using System;

namespace MicaTranslate.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public bool Navigate<TPage>(object? parameter = null) where TPage : Page
    {
        if (_frame == null)
            throw new InvalidOperationException("NavigationService not initialized.");

        return _frame.Navigate(typeof(TPage), parameter);
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack()
    {
        if (CanGoBack)
            _frame!.GoBack();
    }
}
