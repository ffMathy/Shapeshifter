using Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces;
using Shapeshifter.WindowsDesktop.Infrastructure.Events;
using Shapeshifter.WindowsDesktop.Mediators.Interfaces;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Shapeshifter.WindowsDesktop.Controls.Window
{
	/// <summary>
	/// Interaction logic for UserInterfaceControl.xaml
	/// </summary>
	[TemplatePart(Name = "Core", Type = typeof(FrameworkElement))]
	[TemplateVisualState(Name = "InPackagesList", GroupName = "TargetList")]
	[TemplateVisualState(Name = "InActionList", GroupName = "TargetList")]
	public partial class UserInterfaceControl : UserControl, IDisposable
    {
		private IUserInterfaceViewModel viewModel;

		public UserInterfaceControl()
        {
            InitializeComponent();

			VisualStateManager.GoToElementState(this, "InPackagesList", true);
		}

		private void PackageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			PackageList.UpdateLayout();
			if (PackageList.SelectedItem != null)
				PackageList.ScrollIntoView(PackageList.SelectedItem);
		}

		public void Dispose()
		{
			viewModel.UserInterfacePaneSwapped -= ViewModel_UserInterfacePaneSwapped;
			PackageList.SelectionChanged -= PackageList_SelectionChanged;
		}

		public void Initialize(IUserInterfaceViewModel viewModel)
		{
			viewModel.UserInterfacePaneSwapped += ViewModel_UserInterfacePaneSwapped;
			PackageList.SelectionChanged += PackageList_SelectionChanged;

			DataContext = viewModel;

			this.viewModel = viewModel;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
		}

		private void ViewModel_UserInterfacePaneSwapped(object sender, UserInterfacePaneSwappedEventArgument e)
		{
			switch (e.Pane)
			{
				case ClipboardUserInterfacePane.Actions:
					VisualStateManager.GoToElementState(this, "InActionList", true);
					break;

				case ClipboardUserInterfacePane.ClipboardPackages:
					VisualStateManager.GoToElementState(this, "InPackagesList", true);
					break;
			}
		}
	}
}
