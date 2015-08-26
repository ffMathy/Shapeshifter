using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Arguments.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Messages.Interceptors.Hotkeys.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    class Main : ISingleInstance
    {
        readonly IEnumerable<IArgumentProcessor> argumentProcessors;

        readonly IClipboardListWindow mainWindow;
        readonly IWindowMessageHook windowMessageHook;
        readonly IClipboardUserInterfaceMediator clipboardUserInterfaceMediator;
        readonly IKeyInterceptor keyInterceptor;

        public Main(
            IEnumerable<IArgumentProcessor> argumentProcessors,
            IClipboardListWindow mainWindow,
            IWindowMessageHook windowMessageHook,
            IKeyInterceptor keyInterceptor,
            IClipboardUserInterfaceMediator clipboardUserInterfaceMediator)
        {
            this.argumentProcessors = argumentProcessors;
            this.mainWindow = mainWindow;
            this.windowMessageHook = windowMessageHook;
            this.keyInterceptor = keyInterceptor;
            this.clipboardUserInterfaceMediator = clipboardUserInterfaceMediator;
        }

        public void Start(string[] arguments)
        {
            var processorsUsed = ProcessArguments(arguments);
            if (processorsUsed.Any(x => x.Terminates))
            {
                return;
            }

            LaunchMainWindow();
        }

        void LaunchMainWindow()
        {
            mainWindow.SourceInitialized += (sender, e) => SetupServices();
            mainWindow.Show();
        }

        void SetupServices()
        {
            SetupHooks();
            clipboardUserInterfaceMediator.Connect();
        }

        void SetupHooks()
        {
            windowMessageHook.Connect();
            SetupWindowKeyInterception();
        }

        void SetupWindowKeyInterception()
        {
            var mainWindowHandle = windowMessageHook.MainWindowHandle;
            keyInterceptor.AddInterceptingKey(
                mainWindowHandle, KeyboardApi.VK_KEY_UP);
            keyInterceptor.AddInterceptingKey(
                mainWindowHandle, KeyboardApi.VK_KEY_DOWN);
            keyInterceptor.AddInterceptingKey(
                mainWindowHandle, KeyboardApi.VK_KEY_LEFT);
            keyInterceptor.AddInterceptingKey(
                mainWindowHandle, KeyboardApi.VK_KEY_RIGHT);
        }

        IEnumerable<IArgumentProcessor> ProcessArguments(string[] arguments)
        {
            var processorsUsed = new List<IArgumentProcessor>();
            foreach (var processor in argumentProcessors)
            {
                if (processor.CanProcess(arguments))
                {
                    processor.Process(arguments);
                    processorsUsed.Add(processor);
                }
            }

            return processorsUsed;
        }
    }
}
