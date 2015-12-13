namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Factories
{
    using Controls.Window.Interfaces;

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