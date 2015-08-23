using System.Windows;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    /// Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardListWindow : Window, IClipboardListWindow
    {
        readonly IClipboardUserInterfaceMediator mediator;
        readonly IWindowMessageHook windowMessageHook;

        ClipboardListWindow() { }

        public ClipboardListWindow(
            IClipboardListViewModel viewModel,
            IClipboardUserInterfaceMediator mediator,
            IWindowMessageHook windowMessageHook)
        {
            this.mediator = mediator;
            this.windowMessageHook = windowMessageHook;

            SourceInitialized += ClipboardListWindow_SourceInitialized;

            InitializeComponent();

            DataContext = viewModel;
        }

        void ClipboardListWindow_SourceInitialized(object sender, System.EventArgs e)
        {
            windowMessageHook.Connect();
            mediator.Connect();
        }
    }
}
