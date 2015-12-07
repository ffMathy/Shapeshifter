namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces
{
    using System.Windows.Media.Imaging;

    public interface IImageFileInterpreter
    {
        BitmapSource Interpret(byte[] bytes);
    }
}