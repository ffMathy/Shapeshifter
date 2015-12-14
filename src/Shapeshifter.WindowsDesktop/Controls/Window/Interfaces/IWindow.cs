namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System;
    using System.Windows.Interop;

    public interface IWindow
    {
        event EventHandler SourceInitialized;

        void AddHwndSourceHook(HwndSourceHook hook);

        void RemoveHwndSourceHook(HwndSourceHook hook);

        void Show();
    }
}