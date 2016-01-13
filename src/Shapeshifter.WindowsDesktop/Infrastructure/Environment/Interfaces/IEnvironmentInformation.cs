namespace Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces
{
    public interface IEnvironmentInformation
    {
        bool GetIsInDesignTime();

        bool GetIsDebugging();

        bool GetHasInternetAccess();
    }
}