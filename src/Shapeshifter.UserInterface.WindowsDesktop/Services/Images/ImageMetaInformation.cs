#region

using System.Windows.Media;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Images
{
    public struct ImageMetaInformation
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public double DpiX { get; set; }

        public double DpiY { get; set; }

        public PixelFormat PixelFormat { get; set; }
    }
}