namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System;

    using Interfaces;

    class MainWindowHandleContainer: IMainWindowHandleContainer
    {
        public IntPtr Handle { get; set; }
    }
}