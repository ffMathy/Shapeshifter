namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Windows.Media;

    using Interfaces;
    public class ColorBrightnessAdjustmentService: IColorBrightnessAdjustmentService
    {
        public Color AdjustBrightness(Color input, int adjustment)
        {
            return new Color
            {
                A = input.A,
                B = Adjust(input.B, adjustment),
                R = Adjust(input.R, adjustment),
                G = Adjust(input.G, adjustment)
            };
        }

        static byte Adjust(byte channel, int adjustment)
        {
            return (byte)Math.Min(Math.Max(channel + adjustment, 0), 255);
        }
    }
}