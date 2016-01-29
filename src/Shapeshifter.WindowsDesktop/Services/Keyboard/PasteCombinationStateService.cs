namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System.Windows.Input;

    using Interfaces;

    public class PasteCombinationStateService: IPasteCombinationStateService
    {
        readonly IKeyboardManager keyboardManager;

        public PasteCombinationStateService(
            IKeyboardManager keyboardManager)
        {
            this.keyboardManager = keyboardManager;
        }

        public bool IsCombinationPartiallyHeldDown
            => keyboardManager.IsKeyDown(Key.LeftCtrl) || keyboardManager.IsKeyDown(Key.V);

        public bool IsCombinationFullyHeldDown
            => keyboardManager.IsKeyDown(Key.LeftCtrl) && keyboardManager.IsKeyDown(Key.V);
    }
}