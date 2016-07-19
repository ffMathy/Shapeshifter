namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System.Windows.Media;

    public interface IColorBrightnessAdjustmentService
    {
        Color AdjustBrightness(Color input, int adjustment);
    }
}