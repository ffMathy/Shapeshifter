namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System.Windows.Interop;

    using Shared.Controls.Window.Interfaces;

    public interface IHookableWindow: IWindow
    {
        void AddHwndSourceHook(HwndSourceHook hook);

        void RemoveHwndSourceHook(HwndSourceHook hook);
    }
}