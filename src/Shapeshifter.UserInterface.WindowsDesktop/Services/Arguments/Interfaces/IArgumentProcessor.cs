namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces
{
    internal interface IArgumentProcessor
    {
        bool CanProcess(string[] arguments);

        void Process(string[] arguments);

        bool Terminates { get; }
    }
}