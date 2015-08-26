using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    interface IClipboardListWindow : ISingleInstance
    {
        event EventHandler SourceInitialized;

        void Show();
    }
}
