using System.Windows;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    /// Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ClipboardListWindow : Window
    {
        public ClipboardListWindow()
        {
            InitializeComponent();

            DataContext = App.Container.Resolve<ClipboardListViewModel>();
        }
    }
}
