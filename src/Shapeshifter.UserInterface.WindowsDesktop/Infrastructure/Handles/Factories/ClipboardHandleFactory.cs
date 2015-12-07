namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Handles.Factories
{
    using Windows.Interfaces;

    using Handles.Interfaces;

    using Interfaces;

    class ClipboardHandleFactory: IClipboardHandleFactory
    {
        readonly IMainWindowHandleContainer mainWindow;

        public ClipboardHandleFactory(
            IMainWindowHandleContainer mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public IClipboardHandle StartNewSession()
        {
            return new ClipboardHandle(mainWindow);
        }
    }
}