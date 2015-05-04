using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    class ControlEventArgument
    {
        public ControlEventArgument(UIElement control)
        {
            this.Control = control;
        }

        public UIElement Control { get; private set; }
    }
}
