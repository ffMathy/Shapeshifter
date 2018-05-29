namespace Shapeshifter.WindowsDesktop.Services.Window.Interfaces
{
	using System;
	using System.Diagnostics;

	using Services.Interfaces;

	public interface IActiveWindowService: IHookService
    {
        event EventHandler ActiveWindowChanged;
        event EventHandler ActiveWindowProcessChanged;

		IntPtr ActiveWindowHandle { get; }
		string ActiveWindowTitle { get; }

        Process GetProcessFromWindowHandle(IntPtr handle);

        string GetWindowTitleFromWindowHandle(IntPtr handle);
    }
}