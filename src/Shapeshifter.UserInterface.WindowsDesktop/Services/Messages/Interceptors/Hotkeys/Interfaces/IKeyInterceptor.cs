using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces
{
    interface IKeyInterceptor : IHotkeyInterceptor
    {
        void StartInterceptingKey(int keyCode);
        void StopInterceptingKey(int keyCode);
    }
}
