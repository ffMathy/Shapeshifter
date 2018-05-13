namespace Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces
{
	using System.Threading.Tasks;

	public interface INoArgumentProcessor: IArgumentProcessor
    {
        bool CanProcess();

        Task ProcessAsync();
    }
}