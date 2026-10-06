using MicaTranslate.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Windows.System;

namespace MicaTranslate.UI.Controls;

[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed partial class HotkeyPickerControl : UserControl
{
    private bool _isUpdating;

    private List<VirtualKey> _pressedKeys = new();
    public Func<HotkeyModel, bool>? HotkeyAvailabilityChecker { get; set; }
    public ObservableCollection<VirtualKey> PressedKeys { get; } = new();

    public HotkeyPickerControl()
    {
        InitializeComponent();
    }

    #region DependencyProperties

    public HotkeyModel Hotkey
    {
        get => (HotkeyModel)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(
            nameof(Hotkey),
            typeof(HotkeyModel),
            typeof(HotkeyPickerControl),
            new PropertyMetadata(null, OnHotkeyChanged));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(HotkeyPickerControl),
            new PropertyMetadata(string.Empty));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(HotkeyPickerControl),
            new PropertyMetadata(string.Empty));

    public IconElement HeaderIcon
    {
        get => (IconElement)GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    public static readonly DependencyProperty HeaderIconProperty =
        DependencyProperty.Register(
            nameof(HeaderIcon),
            typeof(IconElement),
            typeof(HotkeyPickerControl),
            new PropertyMetadata(null));

    #endregion DependencyProperties

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (HotkeyPickerControl)d;

        control._isUpdating = true;

        control.PressedKeys.Clear();

        if (e.NewValue is HotkeyModel hotkey)
        {
            foreach (var key in hotkey.Keys)
                control.PressedKeys.Add(key);
        }

        control._isUpdating = false;
    }

    private async void OpenDialog(object sender, RoutedEventArgs e)
    {
        var content = new HotkeyPickerDialogContentControl();
        content.HotkeyAvailabilityChecker = HotkeyAvailabilityChecker;

        content.SetKeys(this.PressedKeys);

        var dialog = new ContentDialog
        {
            Title = "Сочетание клавиш",
            PrimaryButtonText = "Сохранить",
            SecondaryButtonText = "Сбросить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Primary,
            Content = content,
            XamlRoot = XamlRoot
        };

        dialog.SecondaryButtonClick += (s, args) =>
        {
            content.SetKeys(this.PressedKeys);

            args.Cancel = true;
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            Hotkey = new HotkeyModel
            {
                Keys = content.PressedKeys.ToList()
            };
            _pressedKeys = content.PressedKeys.ToList();
            RefreshSaved();
        }
    }

    private void RefreshSaved()
    {
        PressedKeys.Clear();

        foreach (var key in _pressedKeys)
        {
            PressedKeys.Add(key);
        }
    }
}