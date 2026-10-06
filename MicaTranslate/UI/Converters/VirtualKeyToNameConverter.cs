using Microsoft.UI.Xaml.Data;
using System;
using System.Diagnostics.CodeAnalysis;
using Windows.System;

namespace MicaTranslate.UI.Converters;

[DynamicallyAccessedMembers(
DynamicallyAccessedMemberTypes.PublicProperties)]
public partial class VirtualKeyToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (VirtualKey)value switch
        {
            VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl => "Ctrl",
            VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift => "Shift",
            VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu => "Alt",

            VirtualKey.Enter => "Enter",
            VirtualKey.Escape => "Esc",
            VirtualKey.Space => "Space",

            VirtualKey.Back => "Backspace",
            VirtualKey.Delete => "Del",
            VirtualKey.PageUp => "PgUp",
            VirtualKey.PageDown => "PgDn",
            VirtualKey.CapitalLock => "CapsLock",

            _ => ((VirtualKey)value).ToString().ToUpper(),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}