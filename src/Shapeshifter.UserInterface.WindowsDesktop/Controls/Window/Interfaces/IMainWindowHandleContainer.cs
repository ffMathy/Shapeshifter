namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Window.Interfaces
{
    using System;

    using Infrastructure.Dependencies.Interfaces;

    public interface IMainWindowHandleContainer: ISingleInstance
    {
        IntPtr Handle { get; set; }
    }
}