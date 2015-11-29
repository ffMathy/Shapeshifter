namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    using System;

    using Infrastructure.Dependencies.Interfaces;

    public interface IMainWindowHandleContainer: ISingleInstance
    {
        IntPtr Handle { get; set; }
    }
}