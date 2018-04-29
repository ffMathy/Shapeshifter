namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
	using Shapeshifter.WindowsDesktop.Data.Interfaces;
	using System;

    public class UserInterfaceDataControlAddedEventArgument : EventArgs {
		public UserInterfaceDataControlAddedEventArgument(IClipboardDataControlPackage package)
		{
			Package = package;
		}

		public IClipboardDataControlPackage Package { get; }
	}
}