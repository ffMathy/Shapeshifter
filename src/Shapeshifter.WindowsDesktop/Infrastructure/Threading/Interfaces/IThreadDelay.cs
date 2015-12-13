namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System.Threading.Tasks;

    public interface IThreadDelay
    {
        Task ExecuteAsync(int millisecondsDelay);
    }
}