namespace Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces
{
    public interface IEnvironmentInformation
    {
        bool IsInDesignTime { get; }

        bool IsDebugging { get; }
    }
}