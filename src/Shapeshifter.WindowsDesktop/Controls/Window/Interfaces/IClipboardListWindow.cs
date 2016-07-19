namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System;

    using Infrastructure.Dependencies.Interfaces;

    public interface IClipboardListWindow
        : IHookableWindow,
          IDisposable,
          ISingleInstance
    {
    }
}