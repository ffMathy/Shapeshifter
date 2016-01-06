namespace Shapeshifter.UserInterface.WindowsDesktop.Installer
{
    using System.Windows;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App: Application {
        void App_OnStartup(object sender, StartupEventArgs e)
        {
            var window = new MainWindow();
        }
    }
}