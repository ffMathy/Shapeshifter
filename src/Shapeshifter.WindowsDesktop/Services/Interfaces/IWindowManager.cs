namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System;
    using System.Diagnostics;

    public interface IWindowManager: IHookService
    {
        event EventHandler ActiveWindowChanged;
        event EventHandler ActiveWindowProcessChanged;

		IntPtr ActiveWindowHandle { get; }

        Process GetProcessFromWindowHandle(IntPtr handle);

        string GetWindowTitleFromWindowHandle(IntPtr handle);
    }
}