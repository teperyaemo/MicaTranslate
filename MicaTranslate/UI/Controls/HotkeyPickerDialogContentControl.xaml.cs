using MicaTranslate.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Windows.System;

namespace MicaTranslate.UI.Controls;

[DynamicallyAccessedMembers(
DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed partial class HotkeyPickerDialogContentControl : UserControl
{
    private VirtualKey _firstKey { get; set; } = VirtualKey.None;
    private bool _isCapturing;

    public ObservableCollection<VirtualKey> PressedKeys { get; set; } = new();
    public Func<HotkeyModel, bool>? HotkeyAvailabilityChecker { get; set; }

    public bool HasConflict
    {
        get => (bool)GetValue(HasConflictProperty);
        set => SetValue(HasConflictProperty, value);
    }

    public static readonly DependencyProperty HasConflictProperty =
        DependencyProperty.Register(
            nameof(HasConflict),
            typeof(bool),
            typeof(HotkeyPickerDialogContentControl),
            new PropertyMetadata(false));

    public HotkeyPickerDialogContentControl()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            RootPanel.Focus(FocusState.Programmatic);
        };
    }

    public void SetKeys(IEnumerable<VirtualKey> keys)
    {
        PressedKeys.Clear();

        foreach (var key in keys)
            PressedKeys.Add(key);

        RootPanel.Focus(FocusState.Programmatic);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var key = NormalizeModifier(e.Key);

        if (!_isCapturing)
        {
            PressedKeys.Clear();
            _firstKey = key;
            _isCapturing = true;
        }

        if (PressedKeys.Contains(key))
        {
            e.Handled = true;
            return;
        }

        if (IsModifier(key))
        {
            PressedKeys.Add(key);
        }
        else
        {
            if (PressedKeys.Count == 0)
                return;

            if (!IsModifier(PressedKeys.Last()))
                PressedKeys.RemoveAt(PressedKeys.Count - 1);

            PressedKeys.Add(key);
        }

        e.Handled = true;

        ValidateHotkey();
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        var key = NormalizeModifier(e.Key);

        if (key == _firstKey)
        {
            _isCapturing = false;

            if (PressedKeys.Any() && !IsPressedKeysValid())
                PressedKeys.Clear();
        }

        ValidateHotkey();
    }

    private bool IsPressedKeysValid()
    {
        if (PressedKeys.Count == 0)
            return false;

        var hasModifier = PressedKeys.Any(k => IsModifier(k));
        var lastKeyIsCommon = !IsModifier(PressedKeys.Last());

        return lastKeyIsCommon && hasModifier;
    }

    private static bool IsModifier(VirtualKey key)
    {
        return key is
            VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl or
            VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift or
            VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu or
            VirtualKey.LeftWindows or VirtualKey.RightWindows;
    }

    private static VirtualKey NormalizeModifier(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.LeftControl or VirtualKey.RightControl => VirtualKey.Control,
            VirtualKey.LeftShift or VirtualKey.RightShift => VirtualKey.Shift,
            VirtualKey.LeftMenu or VirtualKey.RightMenu => VirtualKey.Menu,
            VirtualKey.LeftWindows or VirtualKey.RightWindows => VirtualKey.LeftWindows,
            _ => key
        };
    }

    private void ValidateHotkey()
    {
        if (!IsPressedKeysValid())
        {
            HasConflict = false;
            ConflictInfoBar.IsOpen = false;
            return;
        }

        var hotkey = new HotkeyModel
        {
            Keys = PressedKeys.ToList()
        };

        bool available =
            HotkeyAvailabilityChecker?.Invoke(hotkey)
            ?? true;

        HasConflict = !available;

        ConflictInfoBar.IsOpen = HasConflict;
    }
}