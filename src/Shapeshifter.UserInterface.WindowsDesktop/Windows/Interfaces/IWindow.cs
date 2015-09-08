using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    internal interface IWindow
    {
        event EventHandler SourceInitialized;

        void Show();
    }
}