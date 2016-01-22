namespace Shapeshifter.WindowsDesktop.Services.Screen
{
    using System.Windows;
    using System.Windows.Forms;

    using Interfaces;

    using Services.Interfaces;

    class ScreenManager: IScreenManager
    {
        readonly IPixelConversionService pixelConversionService;

        public ScreenManager(
            IPixelConversionService pixelConversionService)
        {
            this.pixelConversionService = pixelConversionService;
        }

        public ScreenInformation GetPrimaryScreen()
        {
            var screen = Screen.PrimaryScreen;

            var devicePosition = new Vector(
                screen.WorkingArea.X,
                screen.WorkingArea.Y);

            var deviceSize = new Vector(
                screen.WorkingArea.Width,
                screen.WorkingArea.Height);

            var deviceIndependentPosition = pixelConversionService
                .ConvertDeviceToDeviceIndependentPixels(devicePosition);

            var deviceIndependentSize = pixelConversionService
                .ConvertDeviceToDeviceIndependentPixels(deviceSize);

            return new ScreenInformation(
                deviceIndependentPosition,
                deviceIndependentSize);
        }
    }
}