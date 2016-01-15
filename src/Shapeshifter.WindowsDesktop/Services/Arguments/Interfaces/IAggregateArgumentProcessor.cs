namespace Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces
{
    public interface IAggregateArgumentProcessor
    {
        bool ShouldTerminate { get; }

        void ProcessArguments(string[] arguments);
    }
}