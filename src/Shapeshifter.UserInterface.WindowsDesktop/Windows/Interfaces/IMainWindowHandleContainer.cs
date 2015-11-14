using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    public interface IMainWindowHandleContainer : ISingleInstance
    {
        IntPtr Handle { get; set; }
    }
}