namespace Shapeshifter.WindowsDesktop.Services.Images.Interfaces
{
    using System.Windows.Media.Imaging;

    public interface IImageFileInterpreter
    {
        BitmapSource Interpret(byte[] bytes);
    }
}