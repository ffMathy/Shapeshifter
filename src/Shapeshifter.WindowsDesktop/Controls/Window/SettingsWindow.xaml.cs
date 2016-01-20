namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using Interfaces;

    using ViewModels.Interfaces;

    using Window = System.Windows.Window;

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, ISettingsWindow
    {
        public SettingsWindow(
            ISettingsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
