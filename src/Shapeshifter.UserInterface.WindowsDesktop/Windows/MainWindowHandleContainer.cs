using System;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    internal class MainWindowHandleContainer : IMainWindowHandleContainer
    {
        public IntPtr Handle { get; set; }
    }
}