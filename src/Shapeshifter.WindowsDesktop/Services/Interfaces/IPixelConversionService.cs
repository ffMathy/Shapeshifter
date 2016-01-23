namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System.Windows;

    public interface IPixelConversionService
    {
        Vector ConvertDeviceToDeviceIndependentPixels(
            Vector devicePixels);
    }
}