namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces
{
    public interface IEnvironmentInformation
    {
        bool IsInDesignTime { get; }

        bool IsDebugging { get; }
    }
}