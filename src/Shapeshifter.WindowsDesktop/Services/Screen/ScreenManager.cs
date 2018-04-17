namespace Shapeshifter.WindowsDesktop.Services.Screen
{
	using System.Linq;
	using System.Windows;
    using System.Windows.Forms;
	using System.Windows.Input;
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
			return GetScreenInformationFromScreen(screen);
		}

		public ScreenInformation GetActiveScreen()
		{
			var screens = Screen
				.AllScreens
				.Select(GetScreenInformationFromScreen)
				.ToArray();
			var mousePosition = Control.MousePosition;
			return screens.Single(screen => 
				screen.X <= mousePosition.X &&
				screen.Y <= mousePosition.Y &&
				screen.X + screen.Width >= mousePosition.X &&
				screen.Y + screen.Height >= mousePosition.Y);
		}

		private ScreenInformation GetScreenInformationFromScreen(Screen screen)
		{
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