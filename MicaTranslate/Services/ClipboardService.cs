using MicaTranslate.Core.Interfaces.Services;
using Windows.ApplicationModel.DataTransfer;

namespace MicaTranslate.Services;

public class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);

        Clipboard.SetContent(package);
        Clipboard.Flush();
    }
}
