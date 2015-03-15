namespace Shapeshifter.Core.Builders
{
    public interface IClipboardDataControlBuilder<TControlType, TDataType>
        where TDataType : IClipboardData
    {
        TControlType Build(TDataType data);

        /// <summary>
        /// Returns a boolean indicating whether or not a control can be built from the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        bool CanBuild(IClipboardData data);
    }
}
