namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    public interface IKeyboardPasteCombinationStateService
    {
        bool IsCombinationFullyHeldDown { get; }
        bool IsCombinationPartiallyHeldDown { get; }
		bool IsTextKeyDown { get; }
		bool IsModiferKeyDown { get; }
    }
}