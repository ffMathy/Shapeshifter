using System.Windows;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    /// Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardListWindow : Window, IClipboardListWindow
    {
        ClipboardListWindow() { }

        public ClipboardListWindow(
            IClipboardListViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
