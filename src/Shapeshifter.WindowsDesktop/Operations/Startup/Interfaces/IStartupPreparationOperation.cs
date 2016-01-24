namespace Shapeshifter.WindowsDesktop.Operations.Startup.Interfaces
{
    using Operations.Interfaces;

    public interface IStartupPreparationOperation: IOperation
    {
        bool ShouldTerminate { get; }

        string[] Arguments { get; set; }
    }
}