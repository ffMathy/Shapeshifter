using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Shapeshifter.Desktop.Controls;

/// <summary>
/// Shows a copied image as the original Shapeshifter did: only through tilted strips that start a little way into the
/// row, so the area on the left (where the source application's icon is) stays clear and thin gaps run between the
/// strips.
/// </summary>
/// <remarks>
/// Ported from the opacity mask of the WPF version's <c>ClipboardImageDataControl.xaml</c>: eighteen 15-unit-wide lines
/// from (19 + 20i, 0) to (67 + 20i, 110), aligned to the top right of the 92-high image and moved 11.5 % of its width to
/// the right and 5 % of its height up.
/// </remarks>
public sealed class StripedImage : Avalonia.Controls.Control
{
    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<StripedImage, Bitmap?>(nameof(Source));

    private const double DesignHeight = 92;
    private const int StripCount = 18;
    private const double StripSpacing = 20;
    private const double StripWidth = 15;
    private const double TopStart = 19;
    private const double BottomStart = 67;
    private const double MaskHeight = 110;

    static StripedImage()
    {
        AffectsRender<StripedImage>(SourceProperty);
    }

    public StripedImage()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    public Bitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>The strips for an image of the given size (the row's width, 92 high), in the image's coordinates.</summary>
    /// <remarks>
    /// The original brush was not scaled (Stretch=None): its stroked bounds (x 12.1 … 413.9, y -3 … 113) were aligned to
    /// the top right of the image, the 92-high viewport was cut out, and that was moved right by 11.5 % of the width and
    /// up by 5 % of the height. What was moved past the bottom stays white, as does everything left of the strips.
    /// </remarks>
    public static Geometry CreateStrips(Size size)
    {
        var width = size.Width;

        // Half the pen, perpendicular to the lines.
        var dx = BottomStart - TopStart;
        var length = Math.Sqrt((dx * dx) + (MaskHeight * MaskHeight));
        var nx = MaskHeight / length * (StripWidth / 2);
        var ny = dx / length * (StripWidth / 2);

        var strokedRight = TopStart + ((StripCount - 1) * StripSpacing) + dx + nx;
        var strokedTop = -ny;
        var shiftX = 0.115 * width;
        var shiftY = -0.05 * DesignHeight;
        var offsetX = width - strokedRight + shiftX;
        var offsetY = -strokedTop + shiftY;

        var strips = new StreamGeometry();
        using (var geometry = strips.Open())
        {
            for (var i = 0; i < StripCount; i++)
            {
                var top = new Point(TopStart + (i * StripSpacing) + offsetX, offsetY);
                var bottom = new Point(BottomStart + (i * StripSpacing) + offsetX, MaskHeight + offsetY);
                geometry.BeginFigure(new Point(top.X - nx, top.Y + ny), isFilled: true);
                geometry.LineTo(new Point(top.X + nx, top.Y - ny));
                geometry.LineTo(new Point(bottom.X + nx, bottom.Y - ny));
                geometry.LineTo(new Point(bottom.X - nx, bottom.Y + ny));
                geometry.EndFigure(isClosed: true);
            }
        }

        // The brush only covered its own viewport, which the translation moved; below it the image is not shown.
        var viewport = new RectangleGeometry(new Rect(shiftX, shiftY, width, DesignHeight));
        return new CombinedGeometry(GeometryCombineMode.Intersect, strips, viewport);
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var source = Source;
        var bounds = new Rect(Bounds.Size);
        if (source is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        // Fill the row, cropping whatever does not fit (Stretch.UniformToFill), centred.
        var imageSize = source.Size;
        var scale = Math.Max(bounds.Width / imageSize.Width, bounds.Height / imageSize.Height);
        var cropped = new Size(bounds.Width / scale, bounds.Height / scale);
        var sourceRect = new Rect(
            new Point((imageSize.Width - cropped.Width) / 2, (imageSize.Height - cropped.Height) / 2),
            cropped);

        using (context.PushGeometryClip(CreateStrips(bounds.Size)))
        {
            context.DrawImage(source, sourceRect, bounds);
        }
    }
}
