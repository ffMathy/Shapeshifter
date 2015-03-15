namespace Shapeshifter.Core
{
    interface IClipboardData : ISerializable
    {
        IDataSource Source { get; }
    }
}
