using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Path = Avalonia.Controls.Shapes.Path;

namespace Shapeshifter.ScreenshotGenerator;

/// <summary>Artwork for the screenshots: a desktop background, a photo, application icons and file icons.</summary>
internal static class SampleArt
{
    public enum DocumentBadge
    {
        Lines,
        Video,
        Picture,
    }

    /// <summary>A dark blue desktop with soft beams of light.</summary>
    public static byte[] Desktop(int width, int height)
    {
        var canvas = new Canvas
        {
            Width = width,
            Height = height,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#0A3D7A"), 0), new GradientStop(Color.Parse("#052A5C"), 0.55), new GradientStop(Color.Parse("#021333"), 1) },
            },
        };

        for (var i = 0; i < 4; i++)
        {
            canvas.Children.Add(new Path
            {
                Data = Geometry.Parse($"M{width * (0.45 + (i * 0.12))},0 L{width},{height * (0.15 + (i * 0.1))} L{width},{height * (0.3 + (i * 0.1))} L{width * (0.35 + (i * 0.12))},0 Z"),
                Fill = new SolidColorBrush(Color.FromArgb((byte)(28 - (i * 5)), 140, 200, 255)),
            });
        }

        canvas.Children.Add(new Ellipse
        {
            Width = width * 0.9,
            Height = height * 0.5,
            Fill = new RadialGradientBrush
            {
                GradientStops = { new GradientStop(Color.FromArgb(70, 60, 140, 230), 0), new GradientStop(Color.FromArgb(0, 60, 140, 230), 1) },
            },
            [Canvas.LeftProperty] = width * 0.35,
            [Canvas.TopProperty] = height * 0.25,
        });

        return Render(canvas, width, height);
    }

    /// <summary>A sunny forest, standing in for a copied photo.</summary>
    public static byte[] Banner(int width, int height)
    {
        var canvas = new Canvas
        {
            Width = width,
            Height = height,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#E9F2C9"), 0),
                    new GradientStop(Color.Parse("#8DB34A"), 0.35),
                    new GradientStop(Color.Parse("#3F6B22"), 0.7),
                    new GradientStop(Color.Parse("#22401A"), 1),
                },
            },
        };

        canvas.Children.Add(new Ellipse
        {
            Width = height * 1.4,
            Height = height * 1.4,
            Fill = new RadialGradientBrush
            {
                GradientStops = { new GradientStop(Color.FromArgb(230, 255, 255, 235), 0), new GradientStop(Color.FromArgb(0, 255, 255, 235), 1) },
            },
            [Canvas.LeftProperty] = width * 0.28,
            [Canvas.TopProperty] = -height * 0.5,
        });

        // Tree trunks.
        for (var x = 20; x < width; x += 37)
        {
            canvas.Children.Add(new Rectangle
            {
                Width = 3 + (x % 5),
                Height = height,
                Fill = new SolidColorBrush(Color.FromArgb(70, 40, 60, 20)),
                [Canvas.LeftProperty] = x,
            });
        }

        return Render(canvas, width, height);
    }

    /// <summary>A round, colourful browser-like icon.</summary>
    public static byte[] BrowserIcon()
    {
        const int size = 32;
        var canvas = new Canvas { Width = size, Height = size };
        canvas.Children.Add(Pie(Color.Parse("#DB4437"), -90, 30));
        canvas.Children.Add(Pie(Color.Parse("#0F9D58"), 30, 150));
        canvas.Children.Add(Pie(Color.Parse("#F4B400"), 150, 270));
        canvas.Children.Add(new Ellipse { Width = 15, Height = 15, Fill = Brushes.White, [Canvas.LeftProperty] = 8.5, [Canvas.TopProperty] = 8.5 });
        canvas.Children.Add(new Ellipse { Width = 11, Height = 11, Fill = new SolidColorBrush(Color.Parse("#4285F4")), [Canvas.LeftProperty] = 10.5, [Canvas.TopProperty] = 10.5 });
        return Render(canvas, size, size);

        static Path Pie(Color color, double from, double to)
        {
            static Point At(double degrees) => new(16 + (16 * Math.Cos(degrees * Math.PI / 180)), 16 + (16 * Math.Sin(degrees * Math.PI / 180)));
            var start = At(from);
            var end = At(to);
            return new Path
            {
                Data = Geometry.Parse(FormattableString.Invariant($"M16,16 L{start.X},{start.Y} A16,16 0 0 1 {end.X},{end.Y} Z")),
                Fill = new SolidColorBrush(color),
            };
        }
    }

    /// <summary>A purple, ribbon-like editor icon.</summary>
    public static byte[] EditorIcon()
    {
        const int size = 32;
        var canvas = new Canvas { Width = size, Height = size };
        canvas.Children.Add(new Path
        {
            Data = Geometry.Parse("M22,2 L30,6 V26 L22,30 L9,19 L4,23 L1,21.5 V10.5 L4,9 L9,13 Z M22,10 L15,16 L22,22 Z M4,13 V19 L7,16 Z"),
            Fill = new SolidColorBrush(Color.Parse("#865FC5")),
        });
        return Render(canvas, size, size);
    }

    /// <summary>A yellow folder.</summary>
    public static byte[] Folder(int size)
    {
        var canvas = new Canvas { Width = 48, Height = 48 };
        canvas.Children.Add(new Path { Data = Geometry.Parse("M2,8 H18 L22,12 H44 V40 H2 Z"), Fill = new SolidColorBrush(Color.Parse("#E0A526")) });
        canvas.Children.Add(new Path { Data = Geometry.Parse("M2,16 H46 V42 H2 Z"), Fill = new SolidColorBrush(Color.Parse("#FFCF4A")) });
        canvas.Children.Add(new Path { Data = Geometry.Parse("M2,16 H46 V18 H2 Z"), Fill = new SolidColorBrush(Color.Parse("#FFE38A")) });
        return Render(canvas, 48, 48, size);
    }

    /// <summary>A sheet of paper, optionally showing what kind of document it is.</summary>
    public static byte[] Document(int size, DocumentBadge badge)
    {
        var canvas = new Canvas { Width = 48, Height = 48 };
        canvas.Children.Add(new Path
        {
            Data = Geometry.Parse("M9,2 H30 L40,12 V46 H9 Z"),
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.Parse("#9A9A9A")),
            StrokeThickness = 1,
        });
        canvas.Children.Add(new Path { Data = Geometry.Parse("M30,2 V12 H40"), Stroke = new SolidColorBrush(Color.Parse("#9A9A9A")), StrokeThickness = 1 });
        switch (badge)
        {
            case DocumentBadge.Lines:
                for (var y = 16; y <= 40; y += 4)
                {
                    canvas.Children.Add(new Rectangle { Width = 22, Height = 1, Fill = new SolidColorBrush(Color.Parse("#B8B8B8")), [Canvas.LeftProperty] = 13, [Canvas.TopProperty] = y });
                }

                break;
            case DocumentBadge.Video:
                canvas.Children.Add(new Path { Data = Geometry.Parse("M16,18 L34,28 L16,38 Z"), Fill = new SolidColorBrush(Color.Parse("#F28C1E")) });
                break;
            case DocumentBadge.Picture:
                canvas.Children.Add(new Rectangle { Width = 24, Height = 18, Fill = new SolidColorBrush(Color.Parse("#4DA3E8")), [Canvas.LeftProperty] = 12.5, [Canvas.TopProperty] = 20 });
                canvas.Children.Add(new Path { Data = Geometry.Parse("M12.5,38 L20,28 L26,34 L30,30 L36.5,38 Z"), Fill = new SolidColorBrush(Color.Parse("#E6F3FC")) });
                break;
        }

        return Render(canvas, 48, 48, size);
    }

    private static byte[] Render(Control control, int width, int height, int? outputSize = null)
    {
        var scale = outputSize is { } output ? output / (double)width : 1;
        var pixelWidth = (int)Math.Round(width * scale);
        var pixelHeight = (int)Math.Round(height * scale);
        var root = new LayoutTransformControl { LayoutTransform = new ScaleTransform(scale, scale), Child = control };
        root.Measure(new Size(pixelWidth, pixelHeight));
        root.Arrange(new Rect(0, 0, pixelWidth, pixelHeight));
        using var bitmap = new RenderTargetBitmap(new PixelSize(pixelWidth, pixelHeight));
        bitmap.Render(root);
        using var stream = new MemoryStream();
        bitmap.Save(stream, new PngBitmapEncoderOptions());
        return stream.ToArray();
    }
}
