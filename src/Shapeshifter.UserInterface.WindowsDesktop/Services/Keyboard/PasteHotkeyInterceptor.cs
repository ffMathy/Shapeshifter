using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System.Windows.Input;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard
{
    class PasteHotkeyInterceptor : IPasteHotkeyInterceptor
    {
        private bool isVDown;
        private bool isCtrlDown;

        public void ReceiveKeyDown(Key key)
        {
            switch (key)
            {
                case Key.V:
                    isVDown = true;
                    break;

                case Key.LeftCtrl:
                    isCtrlDown = true;
                    break;
            }
        }

        public void ReceiveKeyUp(Key key)
        {
            switch (key)
            {
                case Key.V:
                    isVDown = false;
                    break;

                case Key.LeftCtrl:
                    isCtrlDown = false;
                    break;
            }
        }

        public bool ShouldBlockKeyDown(Key key)
        {
            if(!isCtrlDown || !isVDown)
            {
                return false;
            }

            return key == Key.LeftCtrl || key == Key.V;
        }

        public bool ShouldBlockKeyUp(Key key)
        {
            return false;
        }
    }
}
