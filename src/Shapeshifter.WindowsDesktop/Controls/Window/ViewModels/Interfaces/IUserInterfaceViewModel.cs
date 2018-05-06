namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;

	using Data.Interfaces;

	using Infrastructure.Events;
	using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
	using Services.Screen;

	public interface IUserInterfaceViewModel : INotifyPropertyChanged, ISingleInstance
	{
		event EventHandler<UserInterfaceShownEventArgument> UserInterfaceShown;
		event EventHandler<UserInterfacePaneSwappedEventArgument> UserInterfacePaneSwapped;
		event EventHandler<UserInterfaceHiddenEventArgument> UserInterfaceHidden;
		event EventHandler<UserInterfaceDataControlAddedEventArgument> UserInterfaceDataControlAdded;

		IClipboardDataControlPackage SelectedElement { get; set; }
		IActionViewModel SelectedAction { get; set; }

		ScreenInformation ActiveScreen { get; set; }

		ObservableCollection<IClipboardDataControlPackage> Elements { get; }
		ObservableCollection<IActionViewModel> Actions { get; }
	}
}