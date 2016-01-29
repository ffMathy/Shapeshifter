namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    public interface IPasteCombinationStateService
    {
        bool IsCombinationFullyHeldDown { get; }
        bool IsCombinationPartiallyHeldDown { get; }
    }
}