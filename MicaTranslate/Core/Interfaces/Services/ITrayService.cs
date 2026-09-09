using Microsoft.UI.Xaml;
using System;

namespace MicaTranslate.Core.Interfaces.Services;

public interface ITrayService : IDisposable
{
    bool IsInitialized { get; }

    bool IsVisible { get; }

    bool CenterWindowOnOpen { get; set; }

    string IconToolTip { get; set; }

    Action? OpenSettingsAction { get; set; }

    Action? ExitAction { get; set; }

    Action? LeftClickAction { get; set; }

    void Initialize(Window window);

    void ShowWindow();

    void HideWindow();

    void ToggleWindow();
}