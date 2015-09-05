using System.Windows;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Events;
using System;
using System.Windows.Interop;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    /// Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardListWindow : Window, IClipboardListWindow
    {
        readonly IClipboardUserInterfaceMediator mediator;

        public ClipboardListWindow(
            IClipboardListViewModel viewModel,
            IClipboardUserInterfaceMediator mediator)
        {
            this.mediator = mediator;

            Activated += ClipboardListWindow_Activated;

            InitializeComponent();

            SetupViewModel(viewModel);
        }

        public HwndSource HandleSource
        {
            get
            {
                return PresentationSource.FromVisual(this) as HwndSource;
            }
        }

        void ClipboardListWindow_Activated(object sender, System.EventArgs e)
        {
            Activated -= ClipboardListWindow_Activated;
            Hide();
        }

        void SetupViewModel(
            IClipboardListViewModel viewModel)
        {
            viewModel.UserInterfaceShown += ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceHidden += ViewModel_UserInterfaceHidden;

            DataContext = viewModel;
        }

        void ViewModel_UserInterfaceHidden(
            object sender,
            UserInterfaceHiddenEventArgument e)
        {
            Hide();
        }

        void ViewModel_UserInterfaceShown(
            object sender,
            UserInterfaceShownEventArgument e)
        {
            Show();
        }
    }
}
