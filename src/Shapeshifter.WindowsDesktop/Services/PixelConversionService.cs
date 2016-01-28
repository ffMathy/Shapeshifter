namespace Shapeshifter.WindowsDesktop.Services
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Interop;

    using Interfaces;

    public class PixelConversionService: IPixelConversionService
    {
        public Vector ConvertDeviceToDeviceIndependentPixels(
            Vector devicePixels)
        {
            using (var source =
                new HwndSource(
                    new HwndSourceParameters()))
            {
                Debug.Assert(source.CompositionTarget != null, "source.CompositionTarget != null");

                var transform = source
                    .CompositionTarget
                    .TransformFromDevice;

                return transform
                    .Transform(devicePixels);
            }
        }
    }
}