using Shapeshifter.Core.Data;

namespace Shapeshifter.Core.Actions
{
    public interface IAction<in T>
    {
        string Title { get; }

        string Description { get; }

        bool CanPerform(T clipboardData);

        void Perform(T clipboardData);
    }
}
