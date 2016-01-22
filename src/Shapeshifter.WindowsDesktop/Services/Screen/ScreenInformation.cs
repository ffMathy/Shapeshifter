namespace Shapeshifter.WindowsDesktop.Services.Screen
{
    using System.Windows;

    public class ScreenInformation
    {
        public double X { get; set; }
        public double Y { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        public ScreenInformation() { }

        public ScreenInformation(
            Vector position,
            Vector size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.X;
            Height = size.Y;
        }
    }
}