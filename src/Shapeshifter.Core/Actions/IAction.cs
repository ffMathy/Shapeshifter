using Shapeshifter.Core.Data;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Actions
{
    public interface IAction
    {
        string Title { get; }

        string Description { get; }

        byte Order { get; }

        Task<bool> CanPerformAsync(IClipboardData clipboardData);

        Task PerformAsync(IClipboardData clipboardData);
    }
}
