namespace Shapeshifter.WindowsDesktop.Data.Actions.Interfaces
{
    using System.Threading.Tasks;

    using Data.Interfaces;

    public interface IAction
    {
		string Title { get; }

        byte Order { get; }

        Task<bool> CanPerformAsync(IClipboardDataPackage package);

        Task PerformAsync(IClipboardDataPackage package);
    }
}