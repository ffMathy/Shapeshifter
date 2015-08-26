using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Actions
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
