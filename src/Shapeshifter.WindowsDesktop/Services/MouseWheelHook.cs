using Shapeshifter.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    public class MouseWheelHook: IMouseWheelHook
    {
        IHookableWindow connectedWindow;

        public event EventHandler WheelScrolledDown;

        public event EventHandler WheelScrolledUp;

        public bool IsConnected
            => connectedWindow != null;

        public void Disconnect()
        {
            Mouse.RemovePreviewMouseWheelHandler((DependencyObject)connectedWindow, Handler);

            connectedWindow = null;
        }

        public void Connect(IHookableWindow window)
        {
            Mouse.AddPreviewMouseWheelHandler((DependencyObject)window, Handler);
            this.connectedWindow = window;
        }

        void Handler(object sender, MouseWheelEventArgs mouseWheelEventArgs)
        {
            if (mouseWheelEventArgs.Delta > 0)
            {
                OnWheelScrolledDown();
            } else if (mouseWheelEventArgs.Delta < 0)
            {
                OnWheelScrolledUp();
            }
        }

        protected virtual void OnWheelScrolledDown()
        {
            WheelScrolledDown?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnWheelScrolledUp()
        {
            WheelScrolledUp?.Invoke(this, EventArgs.Empty);
        }
    }
}