namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System.Windows;
    using System.Windows.Input;

    using Interfaces;

    using ViewModels.Interfaces;

    /// <summary>
    ///     Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
        : Window,
          ISettingsWindow
    {
        readonly ISettingsViewModel viewModel;

        public SettingsWindow(
            ISettingsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = this.viewModel = viewModel;
        }

        void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            viewModel.OnReceiveKeyDown(e.Key);
        }
    }
}