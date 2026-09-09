using CommunityToolkit.Mvvm.ComponentModel;
using GTranslate;

namespace MicaTranslate.Core.Models;

public partial class LanguageModel : ObservableObject
{
    public string Name { get; set; }
    public Language TargetLanguage { get; set; }

    [ObservableProperty]
    private bool isFavorite;

    public bool CanFavorite { get; set; } = true;

    [ObservableProperty]
    private string favoriteGlyph = "\uE734";

    partial void OnIsFavoriteChanged(bool value)
    {
        FavoriteGlyph = value ? "\uE735" : "\uE734";
    }
}
