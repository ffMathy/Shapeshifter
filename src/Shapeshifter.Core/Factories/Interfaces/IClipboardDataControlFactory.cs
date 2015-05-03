using Shapeshifter.Core.Data;

namespace Shapeshifter.Core.Factories
{
    public interface IClipboardDataControlFactory
    {
        /// <summary>
        /// Returns the constructed data control for the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        object Build(IClipboardData data);

        /// <summary>
        /// Returns a boolean indicating whether or not a control can be built from the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        bool CanBuild(IClipboardData data);
    }
}
