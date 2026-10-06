using MicaTranslate.Core.Models;
using Microsoft.UI.Xaml;
using System;

namespace MicaTranslate.Core.Interfaces.Services;

public interface IHotkeyService
{
    event Action? HotkeyPressed;

    void Initialize(Window window);

    bool Register(HotkeyModel hotkey);

    void Unregister();

    bool IsAvailable(HotkeyModel hotkey);
}