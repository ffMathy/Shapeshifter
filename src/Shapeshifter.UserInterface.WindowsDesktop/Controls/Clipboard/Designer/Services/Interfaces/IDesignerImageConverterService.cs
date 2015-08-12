using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces
{
    public interface IDesignerImageConverterService
    {
        BitmapSource ConvertFileBytesToBitmapSource(byte[] fileBytes);

        byte[] GenerateDesignerImageBytesFromFileBytes(byte[] fileBytes);
    }
}
