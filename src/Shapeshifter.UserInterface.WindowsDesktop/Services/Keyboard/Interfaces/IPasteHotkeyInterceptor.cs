using System.Windows.Input;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces
{
    public interface IPasteHotkeyInterceptor
    {
        void ReceiveKeyDown(Key key);

        void ReceiveKeyUp(Key key);

        bool ShouldBlockKeyDown(Key key);

        bool ShouldBlockKeyUp(Key key);

        void SendPasteCombination();
    }
}
