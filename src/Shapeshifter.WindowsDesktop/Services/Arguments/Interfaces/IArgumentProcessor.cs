namespace Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces
{
    public interface IArgumentProcessor
    {
        bool CanProcess(string[] arguments);

        void Process(string[] arguments);

        bool Terminates { get; }
    }
}