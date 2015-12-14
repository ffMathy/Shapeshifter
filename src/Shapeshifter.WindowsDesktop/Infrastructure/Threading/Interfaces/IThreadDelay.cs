namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
    using System.Threading.Tasks;

    public interface IThreadDelay
    {
        void Execute(int millisecondsDelay);

        Task ExecuteAsync(int millisecondsDelay);
    }
}