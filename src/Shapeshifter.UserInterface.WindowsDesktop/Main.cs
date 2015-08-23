using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    class Main : ISingleInstance
    {
        readonly IClipboardListWindow mainWindow;
        readonly IClipboardUserInterfaceMediator mediator;
        readonly IWindowMessageHook windowMessageHook;

        public Main(
            IClipboardListWindow mainWindow,
            IClipboardUserInterfaceMediator mediator,
            IWindowMessageHook windowMessageHook)
        {
            this.mainWindow = mainWindow;
            this.mediator = mediator;
            this.windowMessageHook = windowMessageHook;
        }

        public void Start()
        {
            mainWindow.Show();
            windowMessageHook.Connect();
            mediator.Connect();
        }
    }
}
