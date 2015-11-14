#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    internal class MainWindowHandleContainer : IMainWindowHandleContainer
    {
        public IntPtr Handle { get; set; }
    }
}