namespace Shapeshifter.WindowsDesktop.Startup
{
    using System.Threading.Tasks;

    using Controls.Window.Interfaces;

    using Interfaces;

    using Mediators.Interfaces;

    public class MainWindowPreparationOperation: IMainWindowPreparationOperation
    {
        readonly IClipboardListWindow mainWindow;
        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;

        public MainWindowPreparationOperation(
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator)
        {
            this.mainWindow = mainWindow;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
        }

        public async Task RunAsync()
        {
            mainWindow.SourceInitialized +=
                (sender, e) => clipboardUserInterfaceMediator.Connect(mainWindow);
            mainWindow.Show();
        }
    }
}