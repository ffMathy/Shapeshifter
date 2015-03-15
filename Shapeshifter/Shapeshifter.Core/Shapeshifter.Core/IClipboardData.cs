namespace Shapeshifter.Core
{
    public interface IClipboardData : ISerializable
    {
        IDataSource Source { get; }
    }
}
