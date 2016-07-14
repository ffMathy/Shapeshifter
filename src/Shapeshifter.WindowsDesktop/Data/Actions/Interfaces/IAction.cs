namespace Shapeshifter.WindowsDesktop.Data.Actions.Interfaces
{
    using System.Threading.Tasks;

    using Data.Interfaces;

    public interface IAction
    {
        string Title { get; }

        Task<string> GetDescriptionAsync(IClipboardDataPackage package);

        byte Order { get; }

        Task<bool> CanPerformAsync(IClipboardDataPackage package);

        Task PerformAsync(IClipboardDataPackage package);
    }
}