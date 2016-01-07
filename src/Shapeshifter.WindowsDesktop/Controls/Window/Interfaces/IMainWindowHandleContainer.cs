namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
    using System;

    using Shared.Infrastructure.Dependencies.Interfaces;

    public interface IMainWindowHandleContainer: ISingleInstance
    {
        IntPtr Handle { get; set; }
    }
}