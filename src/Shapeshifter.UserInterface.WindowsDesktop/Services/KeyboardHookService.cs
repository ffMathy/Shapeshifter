using System;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class KeyboardHookService : IKeyboardHookService
    {
        public bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<HookDisconnectedEvent> HookDisconnected;
        public event EventHandler<HookReconnectedEvent> HookReconnected;

        public void Connect()
        {
            throw new NotImplementedException();
        }
    }
}
