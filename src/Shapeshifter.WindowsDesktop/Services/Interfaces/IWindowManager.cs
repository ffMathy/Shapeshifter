namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System;
    using System.Diagnostics;

    public interface IWindowManager: IHookService
    {
        event EventHandler ActiveWindowChanged;
        event EventHandler ActiveWindowProcessChanged;

        Process GetActiveWindowProcess();

        string GetActiveWindowProcessTitle();
    }
}