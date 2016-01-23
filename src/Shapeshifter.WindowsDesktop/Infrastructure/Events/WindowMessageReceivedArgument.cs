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

        public IntPtr WindowHandle { get; private set; }

        public Message Message { get; private set; }

        public IntPtr WordParameter { get; private set; }

        public IntPtr LongParameter { get; private set; }
    }
}