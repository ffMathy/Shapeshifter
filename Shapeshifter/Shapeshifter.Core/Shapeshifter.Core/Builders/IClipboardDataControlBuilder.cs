using Shapeshifter.Core.Data;

namespace Shapeshifter.Core.Builders
{
    public interface IClipboardDataControlBuilder<TControlType>
    {
        TControlType Build(IClipboardData data);

        /// <summary>
        /// Returns a boolean indicating whether or not a control can be built from the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        bool CanBuild(IClipboardData data);
    }
}
