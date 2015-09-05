using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System;
using System.Windows;
using System.Windows.Interop;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    interface IClipboardListWindow : ISingleInstance
    {
        event EventHandler SourceInitialized;

        HwndSource HandleSource { get; }

        void Show();
    }
}
