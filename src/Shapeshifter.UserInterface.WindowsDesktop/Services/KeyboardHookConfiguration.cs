using Microsoft.Win32;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    class KeyboardHookConfiguration : IKeyboardHookConfiguration
    {
        public KeyboardHookConfiguration()
        {
            //get the default hook timeout.
            using (var controlPanelDesktopKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
            {
                HookTimeout = (int)controlPanelDesktopKey.GetValue("LowLevelHooksTimeout", 200);
            }
        }

        public int HookTimeout
        {
            get;private set;
        }
    }
}
