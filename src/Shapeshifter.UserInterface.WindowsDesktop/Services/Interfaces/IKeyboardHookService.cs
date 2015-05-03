using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IKeyboardHookService
    {
        event EventHandler<HookDisconnectedEvent> HookDisconnected;
        event EventHandler<HookReconnectedEvent> HookReconnected;

        bool IsConnected { get; }

        void Connect();


    }
}
