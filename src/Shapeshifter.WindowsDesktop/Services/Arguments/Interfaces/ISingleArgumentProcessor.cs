using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces
{
    public interface ISingleArgumentProcessor: IArgumentProcessor
    {
        bool CanProcess(string[] arguments);

        Task ProcessAsync(string[] arguments);
    }
}