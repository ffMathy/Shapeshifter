namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces
{
    interface IArgumentProcessor
    {
        bool CanProcess(string[] arguments);

        void Process(string[] arguments);

        bool Terminates { get; }
    }
}
