namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System.Windows;

    using Interfaces;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window, IWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}