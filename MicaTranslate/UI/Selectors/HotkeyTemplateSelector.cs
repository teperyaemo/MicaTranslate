using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace MicaTranslate.UI.Selectors;

public partial class HotkeyTemplateSelector : DataTemplateSelector
{
    public DataTemplate KeyTemplate { get; set; }
    public DataTemplate WinKeyTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplate(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplate(item);
    }

    private new DataTemplate SelectTemplate(object item)
    {
        if (item == null)
            return KeyTemplate;

        var key = (VirtualKey)item;

        return key is VirtualKey.LeftWindows or VirtualKey.RightWindows
            ? WinKeyTemplate
            : KeyTemplate;
    }
}