#region

using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Actions.Interfaces
{
    public interface IAction
    {
        string Title { get; }

        string Description { get; }

        byte Order { get; }

        Task<bool> CanPerformAsync(IClipboardDataPackage package);

        Task PerformAsync(IClipboardDataPackage package);
    }
}