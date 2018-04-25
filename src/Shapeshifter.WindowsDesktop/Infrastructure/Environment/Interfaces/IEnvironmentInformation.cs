namespace Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces
{
    public interface IEnvironmentInformation
    {
        bool GetIsInDesignTime();
		bool GetIsRunningDeveloperVersion();

		bool GetIsDebugging();
        bool GetShouldUpdate();

        bool GetHasInternetAccess();
    }
}