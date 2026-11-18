using Avalonia;
using Avalonia.Controls;

namespace Shapeshifter.Desktop.Views;

/// <summary>
/// Measures its child no wider than <see cref="Source"/> wants to be, so the child never makes a window that sizes to
/// its content wider. The picker's footer uses it: its key hints wrap onto another line instead of stretching the
/// picker beyond the history and the actions.
/// </summary>
public sealed class WidthFollower : Decorator
{
    public static readonly StyledProperty<Control?> SourceProperty =
        AvaloniaProperty.Register<WidthFollower, Control?>(nameof(Source));

    private Control? _source;
    private double _sourceWidth;

    static WidthFollower()
    {
        AffectsMeasure<WidthFollower>(SourceProperty);
    }

    public Control? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != SourceProperty)
        {
            return;
        }

        if (_source is not null)
        {
            _source.LayoutUpdated -= OnSourceLayoutUpdated;
        }

        _source = Source;
        if (_source is not null)
        {
            _source.LayoutUpdated += OnSourceLayoutUpdated;
        }
    }

    // The source's desired size is not a property, so a change in it does not invalidate this; check after each layout.
    private void OnSourceLayoutUpdated(object? sender, EventArgs e)
    {
        if (_source is not null && !_source.DesiredSize.Width.Equals(_sourceWidth))
        {
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _sourceWidth = _source?.IsVisible == true ? _source.DesiredSize.Width : 0;
        var width = _sourceWidth > 0 ? Math.Min(availableSize.Width, _sourceWidth) : availableSize.Width;
        var desired = base.MeasureOverride(availableSize.WithWidth(width));
        return double.IsInfinity(width) ? desired : desired.WithWidth(Math.Min(desired.Width, width));
    }
}
