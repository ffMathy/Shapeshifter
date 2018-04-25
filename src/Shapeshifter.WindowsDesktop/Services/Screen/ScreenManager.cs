namespace Shapeshifter.WindowsDesktop.Services.Screen
{
	using System.Linq;
	using System.Windows;
	using System.Windows.Forms;
	using Interfaces;
	using Serilog.Context;
	using Services.Interfaces;

	class ScreenManager : IScreenManager
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

			var deviceMousePosition = Control.MousePosition;
			var deviceIndependentMousePosition = pixelConversionService
				.ConvertDeviceToDeviceIndependentPixels(
					new Vector(
						deviceMousePosition.X,
						deviceMousePosition.Y));

			using (CrossThreadLogContext.Add(nameof(deviceIndependentMousePosition), deviceIndependentMousePosition))
			using (CrossThreadLogContext.Add(nameof(screens), screens)) { 
				var activeScreen = screens.First(screen =>
					screen.Bounds.X <= deviceIndependentMousePosition.X &&
					screen.Bounds.Y <= deviceIndependentMousePosition.Y &&
					screen.Bounds.X + screen.Bounds.Width >= deviceIndependentMousePosition.X &&
					screen.Bounds.Y + screen.Bounds.Height >= deviceIndependentMousePosition.Y);
				return activeScreen;
			}
		}

		private ScreenInformation GetScreenInformationFromScreen(Screen screen)
		{
			return new ScreenInformation(
				GatherScreenBounds(screen.Bounds),
				GatherScreenBounds(screen.WorkingArea));
		}

		private ScreenBounds GatherScreenBounds(System.Drawing.Rectangle inputBounds)
		{
			var devicePosition = new Vector(
				inputBounds.X,
				inputBounds.Y);

			var deviceSize = new Vector(
				inputBounds.Width,
				inputBounds.Height);

			var deviceIndependentPosition = pixelConversionService
				.ConvertDeviceToDeviceIndependentPixels(devicePosition);

			var deviceIndependentSize = pixelConversionService
				.ConvertDeviceToDeviceIndependentPixels(deviceSize);

			return new ScreenBounds(
				deviceIndependentPosition,
				deviceIndependentSize);
		}
	}
}