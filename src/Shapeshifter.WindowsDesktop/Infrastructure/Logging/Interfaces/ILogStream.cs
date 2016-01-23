namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging.Interfaces
{
    using Dependencies.Interfaces;

    public interface ILogStream: ISingleInstance
    {
        void WriteLine(string input);
    }
}