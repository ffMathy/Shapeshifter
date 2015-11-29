namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Interop;

    using Windows.Interfaces;

    using Api;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    class WindowMessageHook
        : IWindowMessageHook,
          IDisposable
    {
        HwndSource hooker;

        IWindow connectedWindow;

        readonly IEnumerable<IWindowMessageInterceptor> windowMessageInterceptors;

        readonly Queue<WindowMessageReceivedArgument> pendingMessages;

        readonly CancellationTokenSource cancellationTokenSource;

        readonly ILogger logger;

        readonly IConsumerThreadLoop consumerLoop;

        public bool IsConnected { get; private set; }

        public WindowMessageHook(
            IReadOnlyCollection<IWindowMessageInterceptor> windowMessageInterceptors,
            ILogger logger,
            IConsumerThreadLoop consumerLoop)
        {
            this.windowMessageInterceptors = windowMessageInterceptors;
            this.logger = logger;
            this.consumerLoop = consumerLoop;

            pendingMessages = new Queue<WindowMessageReceivedArgument>();
            cancellationTokenSource = new CancellationTokenSource();

            logger.Information(
                               $"Window message hook was constructed using {windowMessageInterceptors.Count} interceptors.");
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
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
            hooker.RemoveHook(WindowHookCallback);
        }

        void UninstallInterceptors()
        {
            foreach (var interceptor in windowMessageInterceptors)
            {
                interceptor.Uninstall();
            }
        }

        public void Connect(IWindow target)
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
            foreach (var interceptor in windowMessageInterceptors)
            {
                var messageName = FormatMessage(nextMessage.Message);
                var interceptorName = interceptor.GetType()
                                                 .Name;

                logger.Information(
                                   $"Passing message {messageName} to interceptor {interceptorName}.");

                interceptor.ReceiveMessageEvent(nextMessage);

                logger.Information(
                                   $"Message of type {messageName} passed to interceptor {interceptorName}.");
            }
        }

        void InstallInterceptors()
        {
            var mainWindowHandle = hooker.Handle;
            foreach (var interceptor in windowMessageInterceptors)
            {
                interceptor.Install(mainWindowHandle);
                logger.Information($"Installed interceptor {interceptor.GetType() .Name}.");
            }
        }

        void InstallWindowMessageHook()
        {
            var hooker = connectedWindow.HandleSource;
            hooker.AddHook(WindowHookCallback);

            logger.Information("Installed message hook.");

            this.hooker = hooker;
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
            hooker?.Dispose();
        }
    }
}