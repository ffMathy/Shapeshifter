namespace Shapeshifter.WindowsDesktop.Services.Arguments.Interfaces
{
    public interface ISingleArgumentProcessor : IArgumentProcessor
    {
        bool CanProcess(string[] arguments);

        void Process(string[] arguments);
    }
}