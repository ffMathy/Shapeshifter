using Shapeshifter.WindowsDesktop.Data.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.WindowsDesktop.Services.Screen;
using System;
using System.ComponentModel;

namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
	public interface ISourceClipboardQuantityOverlayViewModel : INotifyPropertyChanged, IDisposable
	{
		event EventHandler<DataSourceClipboardQuantityShownEventArgument> ClipboardQuantityShown;
		event EventHandler<DataSourceClipboardQuantityHiddenEventArgument> ClipboardQuantityHidden;

		ScreenInformation ActiveScreen { get; }

		int Count { get; }

		IDataSource Source { get; }
	}
}