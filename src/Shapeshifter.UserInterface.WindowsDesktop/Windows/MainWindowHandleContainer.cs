namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    using System;

    using Interfaces;

    class MainWindowHandleContainer: IMainWindowHandleContainer
    {
        public IntPtr Handle { get; set; }
    }
}