using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;

namespace MicaTranslate.Core.Interfaces.Services;

public delegate void WindowMessageReceivedHandler(
    nint hWnd,
    uint message,
    nint wParam,
    nint lParam);

public interface IWindowService
{
    event WindowMessageReceivedHandler? WindowMessageReceived;

    event EventHandler<AppWindowClosingEventArgs>? Closing;

    void Initialize(Window window);

    void Resize(int width, int height);

    void Center();

    void Show();

    void Hide();

    bool IsVisible();

    void SetAlwaysOnTop(bool value);

    bool IsAlwaysOnTop();

    void DisableResize();

    void RemoveCaptionButtons();

    void HideFromTaskbar();

    void DisableTitleBarDoubleClick();
}