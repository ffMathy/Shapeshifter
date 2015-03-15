namespace Shapeshifter.Core.Controls
{
    public interface IClipboardItemControl<TControlType>
    {
        TControlType Header { get; set; }
        TControlType Body { get; set; }
        TControlType Source { get; set; }
        TControlType BackgroundImage { get; set; }
    }
}
