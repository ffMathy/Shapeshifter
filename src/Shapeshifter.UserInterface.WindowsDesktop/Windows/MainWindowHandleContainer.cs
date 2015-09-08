using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    class MainWindowHandleContainer : IMainWindowHandleContainer
    {
        public IntPtr Handle
        {
            get; set;
        }
    }
}
