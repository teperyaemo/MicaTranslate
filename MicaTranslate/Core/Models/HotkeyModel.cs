using System.Collections.Generic;
using Windows.System;

namespace MicaTranslate.Core.Models;

public class HotkeyModel
{
    public List<VirtualKey> Keys { get; set; } = new();

    public override string ToString()
    {
        return string.Join(" + ", Keys);
    }
}