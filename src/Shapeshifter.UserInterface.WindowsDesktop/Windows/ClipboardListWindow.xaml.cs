using System.Windows;
using Autofac;
using Shapeshifter.Core.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.ViewModels;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows
{
    /// <summary>
    /// Interaction logic for ClipboardListWindow.xaml
    /// </summary>
    public partial class ClipboardListWindow : Window
    {
        public ClipboardListWindow()
        {
            InitializeComponent();

            DataContext = InversionOfControlHelper.Container.Resolve<ClipboardListViewModel>();
        }
    }
}
