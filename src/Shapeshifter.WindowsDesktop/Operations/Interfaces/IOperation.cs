namespace Shapeshifter.WindowsDesktop.Operations.Interfaces
{
    using System.Threading.Tasks;

    public interface IOperation
    {
        Task RunAsync();
    }
}