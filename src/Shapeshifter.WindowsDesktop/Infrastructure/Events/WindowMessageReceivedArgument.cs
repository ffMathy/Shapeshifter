namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;

    using Services.Messages;

    public class WindowMessageReceivedArgument: EventArgs
    {
        public WindowMessageReceivedArgument(
            IntPtr windowHandle,
            Message message,
            IntPtr wordParameter,
            IntPtr longParameter)
        {
            WindowHandle = windowHandle;
            Message = message;
            WordParameter = wordParameter;
            LongParameter = longParameter;
        }

        public IntPtr WindowHandle { get; }

        public Message Message { get; }

        public IntPtr WordParameter { get; }

        public IntPtr LongParameter { get; }
    }
}