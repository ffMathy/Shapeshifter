namespace Shapeshifter.WindowsDesktop.Services.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Controls.Window.Interfaces;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    class WindowMessageHook
        : IWindowMessageHook,
          IDisposable
    {
        IHookableWindow connectedWindow;

        readonly IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors;

        readonly Queue<WindowMessageReceivedArgument> pendingMessages;

        readonly CancellationTokenSource cancellationTokenSource;

        readonly ILogger logger;
        readonly IConsumerThreadLoop consumerLoop;
        readonly IMainWindowHandleContainer mainWindowHandleContainer;

        public bool IsConnected { get; private set; }

        public WindowMessageHook(
            IReadOnlyCollection<IWindowMessageInterceptor> windowMessageInterceptors,
            ILogger logger,
            IConsumerThreadLoop consumerLoop,
            IMainWindowHandleContainer mainWindowHandleContainer)
        {
            this.windowMessageInterceptors = windowMessageInterceptors;
            this.logger = logger;
            this.consumerLoop = consumerLoop;
            this.mainWindowHandleContainer = mainWindowHandleContainer;

            pendingMessages = new Queue<WindowMessageReceivedArgument>();
            cancellationTokenSource = new CancellationTokenSource();

            logger.Information(
                $"Window message hook was constructed using {windowMessageInterceptors.Count} interceptors.");
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Can't disconnect the hook when it is already disconnected.");
            }

            UninstallInterceptors();
            UninstallWindowMessageHook();

            StopMessageConsumer();

            IsConnected = false;
        }

        void StopMessageConsumer()
        {
            cancellationTokenSource.Cancel();
        }

        void UninstallWindowMessageHook()
        {
            connectedWindow.RemoveHwndSourceHook(WindowHookCallback);
        }

        void UninstallInterceptors()
        {
            foreach (var interceptor in windowMessageInterceptors)
            {
                interceptor.Uninstall();
            }
        }

        public void Connect(IHookableWindow target)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The window message hook has already been connected.");
            }

            connectedWindow = target;

            InstallWindowMessageHook();
            InstallInterceptors();

            IsConnected = true;
        }

        async Task HandleNextMessageAsync()
        {
            var nextMessage = pendingMessages.Dequeue();
            using (logger.Indent())
                foreach (var interceptor in windowMessageInterceptors)
                {
                    var messageName = FormatMessage(nextMessage.Message);
                    var interceptorName = interceptor.GetType()
                                                     .Name;

                    logger.Information(
                        $"Passing message {messageName} to interceptor {interceptorName}.");

                    interceptor.ReceiveMessageEvent(nextMessage);
                }
        }

        void InstallInterceptors()
        {
            var mainWindowHandle = mainWindowHandleContainer.Handle;
            foreach (var interceptor in windowMessageInterceptors)
            {
                interceptor.Install(mainWindowHandle);
                logger.Information($"Installed interceptor {interceptor.GetType() .Name}.");
            }
        }

        void InstallWindowMessageHook()
        {
            connectedWindow.AddHwndSourceHook(WindowHookCallback);
            logger.Information("Installed message hook.");
        }

        IntPtr WindowHookCallback(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (!Enum.IsDefined(typeof (Message), msg))
            {
                return IntPtr.Zero;
            }

            logger.Information(
                $"Message received: [{hwnd}, {FormatMessage((Message) msg)}, {wParam}, {lParam}]");

            var argument = new WindowMessageReceivedArgument(hwnd, (Message) msg, wParam, lParam);
            pendingMessages.Enqueue(argument);

            consumerLoop.Notify(HandleNextMessageAsync, cancellationTokenSource.Token);

            return IntPtr.Zero;
        }

        static string FormatMessage(Message msg)
        {
            return Enum.GetName(typeof (Message), msg);
        }

        public void Dispose()
        {
            consumerLoop.Stop();
        }
    }
}