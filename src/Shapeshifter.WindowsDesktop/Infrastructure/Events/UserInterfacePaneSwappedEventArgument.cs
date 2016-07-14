namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;

    using Mediators.Interfaces;

    public class UserInterfacePaneSwappedEventArgument: EventArgs
    {
        public UserInterfacePaneSwappedEventArgument(ClipboardUserInterfacePane pane)
        {
            Pane = pane;
        }

        public ClipboardUserInterfacePane Pane { get; }
    }
}