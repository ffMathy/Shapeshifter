namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging.Interfaces
{
    using Dependencies.Interfaces;
	using System.Threading.Tasks;

	public interface ILogStream: ISingleInstance
    {
        Task WriteLineAsync(string input);
    }
}