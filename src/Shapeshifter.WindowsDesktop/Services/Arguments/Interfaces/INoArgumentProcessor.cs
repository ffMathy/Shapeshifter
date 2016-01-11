namespace Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces
{
    public interface INoArgumentProcessor : IArgumentProcessor
    {
        bool CanProcess();

        void Process();
    }
}