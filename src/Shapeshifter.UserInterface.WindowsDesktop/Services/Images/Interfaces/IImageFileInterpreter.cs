#region

using System.Windows.Media.Imaging;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces
{
    public interface IImageFileInterpreter
    {
        BitmapSource Interpret(byte[] bytes);
    }
}