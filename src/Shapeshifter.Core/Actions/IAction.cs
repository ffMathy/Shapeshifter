using Shapeshifter.Core.Data;

namespace Shapeshifter.Core.Actions
{
    public interface IAction
    {
        string Title { get; }

        string Description { get; }

        bool CanPerform(IClipboardData clipboardData);

        void Perform(IClipboardData clipboardData);
    }
}
