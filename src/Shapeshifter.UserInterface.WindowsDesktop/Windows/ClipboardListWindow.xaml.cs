using System.Windows;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    /// Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardListWindow : Window
    {
        private ClipboardListWindow() { }

        public ClipboardListWindow(
            IClipboardListViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
