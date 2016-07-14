namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System;
    using System.Threading.Tasks;

    using Infrastructure.Dependencies.Interfaces;

    public interface IClipboardListWindow
        : IHookableWindow,
          IDisposable,
          ISingleInstance
    {
    }
}