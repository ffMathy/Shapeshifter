using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Startup.Interfaces
{
    public interface IStartupOperation
    {
        Task RunAsync();
    }
}