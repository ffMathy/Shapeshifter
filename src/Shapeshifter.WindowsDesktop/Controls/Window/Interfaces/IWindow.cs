namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System;
    using System.Windows.Interop;

    public interface IWindow
    {
        event EventHandler SourceInitialized;

        HwndSource HandleSource { get; }

        void Show();
    }
}