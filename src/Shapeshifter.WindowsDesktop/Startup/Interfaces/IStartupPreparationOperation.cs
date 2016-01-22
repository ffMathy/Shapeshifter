namespace Shapeshifter.WindowsDesktop.Startup.Interfaces
{
    public interface IStartupPreparationOperation : IStartupOperation
    {
        bool ShouldTerminate { get; }

        string[] Arguments { get; set; }
    }
}