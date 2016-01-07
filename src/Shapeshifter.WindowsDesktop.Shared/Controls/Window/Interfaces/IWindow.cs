namespace Shapeshifter.WindowsDesktop.Shared.Controls.Window.Interfaces
{
    using System;

    public interface IWindow
    {
        event EventHandler SourceInitialized;

        void Show();
    }
}