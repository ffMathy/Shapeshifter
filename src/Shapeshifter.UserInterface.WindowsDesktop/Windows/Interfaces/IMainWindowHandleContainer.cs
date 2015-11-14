#region

using System;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    public interface IMainWindowHandleContainer : ISingleInstance
    {
        IntPtr Handle { get; set; }
    }
}