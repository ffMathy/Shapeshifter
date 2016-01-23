namespace Shapeshifter.WindowsDesktop.Operations.Startup.Interfaces
{
    using Operations.Interfaces;

    public interface IPreparationOperation : IOperation
    {
        bool ShouldTerminate { get; }

        string[] Arguments { get; set; }
    }
}