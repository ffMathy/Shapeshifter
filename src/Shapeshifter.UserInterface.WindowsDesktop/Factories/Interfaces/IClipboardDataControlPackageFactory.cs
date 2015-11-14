#region

using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    public interface IClipboardDataControlPackageFactory
    {
        IClipboardDataControlPackage Create();
    }
}