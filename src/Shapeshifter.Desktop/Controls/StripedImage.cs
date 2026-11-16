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
/// from (19 + 20i, 0) to (67 + 20i, 110), right-aligned in a row 92 units high and moved 11.5 % of the row's width to
/// the right and 5 % of its height up. Everything scales with the row's height.
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

    /// <summary>The strips for a row of the given size, in the row's coordinates.</summary>
    public static Geometry CreateStrips(Size size)
    {
        var scale = size.Height / DesignHeight;
        var width = size.Width / scale;

        // The mask's lines span x = 19 - 7.5 … 407 + 7.5; right-aligned, then moved right by 11.5 % and up by 5 %.
        var maskRight = TopStart + ((StripCount - 1) * StripSpacing) + (BottomStart - TopStart) + (StripWidth / 2);
        var offsetX = width - maskRight + (0.115 * width);
        var offsetY = -0.05 * DesignHeight;

        // Half the line width, perpendicular to the lines.
        var dx = BottomStart - TopStart;
        var length = Math.Sqrt((dx * dx) + (MaskHeight * MaskHeight));
        var nx = MaskHeight / length * (StripWidth / 2);
        var ny = -dx / length * (StripWidth / 2);

        var strips = new StreamGeometry();
        using (var geometry = strips.Open())
        {
            for (var i = 0; i < StripCount; i++)
            {
                var top = new Point(TopStart + (i * StripSpacing) + offsetX, offsetY);
                var bottom = new Point(BottomStart + (i * StripSpacing) + offsetX, MaskHeight + offsetY);
                geometry.BeginFigure(Scale(new Point(top.X - nx, top.Y - ny)), isFilled: true);
                geometry.LineTo(Scale(new Point(top.X + nx, top.Y + ny)));
                geometry.LineTo(Scale(new Point(bottom.X + nx, bottom.Y + ny)));
                geometry.LineTo(Scale(new Point(bottom.X - nx, bottom.Y - ny)));
                geometry.EndFigure(isClosed: true);
            }
        }

        return strips;

        Point Scale(Point point) => new(point.X * scale, point.Y * scale);
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
