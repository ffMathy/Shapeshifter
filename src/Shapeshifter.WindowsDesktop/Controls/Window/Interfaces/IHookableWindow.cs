namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System.Windows.Interop;

    public interface IHookableWindow: IWindow
    {
        void AddHwndSourceHook(HwndSourceHook hook);

        void RemoveHwndSourceHook(HwndSourceHook hook);
    }
}