namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System.Windows;

    using Interfaces;

    using ViewModels.Interfaces;

    /// <summary>
    ///     Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
        : Window,
          ISettingsWindow
    {
        public SettingsWindow(
            ISettingsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}