namespace MicaTranslate.Core.Interfaces;

public interface IPageFor<TViewModel> where TViewModel : class
{
    object DataContext { get; set; }
}