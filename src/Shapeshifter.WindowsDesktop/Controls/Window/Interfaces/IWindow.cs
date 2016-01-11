namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System;

    public interface IWindow
    {
        event EventHandler SourceInitialized;

        void Show();
    }
}