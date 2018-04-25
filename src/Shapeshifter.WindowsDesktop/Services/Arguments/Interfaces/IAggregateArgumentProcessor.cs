using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces
{
    public interface IAggregateArgumentProcessor
    {
        bool ShouldTerminate { get; }

        Task ProcessArgumentsAsync(string[] arguments);
    }
}