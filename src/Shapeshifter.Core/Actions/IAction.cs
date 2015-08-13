using Shapeshifter.Core.Data;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Actions
{
    public interface IAction
    {
        string Title { get; }

        string Description { get; }

        bool CanPerform(IClipboardData clipboardData);

        Task PerformAsync(IClipboardData clipboardData);
    }
}
