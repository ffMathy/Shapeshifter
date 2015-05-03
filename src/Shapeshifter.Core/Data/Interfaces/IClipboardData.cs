namespace Shapeshifter.Core.Data
{
    public interface IClipboardData : ISerializable
    {
        IDataSource Source { get; }
    }
}
