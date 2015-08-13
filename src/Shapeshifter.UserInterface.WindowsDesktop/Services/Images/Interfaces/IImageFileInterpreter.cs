using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces
{
    interface IImageFileInterpreter
    {
        BitmapSource Interpret(byte[] bytes);
    }
}
