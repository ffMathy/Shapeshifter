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

        public event EventHandler WheelTilted;

        public bool IsConnected
            => connectedWindow != null;

        public void Disconnect()
        {
            Mouse.RemovePreviewMouseWheelHandler((DependencyObject)connectedWindow, MouseWheelHandler);
            Mouse.RemovePreviewMouseDownHandler((DependencyObject)connectedWindow, MouseDownHandler);

            connectedWindow = null;
        }

        void MouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if ((e.XButton1 == MouseButtonState.Pressed) ||
                (e.XButton2 == MouseButtonState.Pressed))
            {
                OnWheelTilted();
            }
        }

        public void Connect(IHookableWindow window)
        {
            Mouse.AddPreviewMouseWheelHandler((DependencyObject)window, MouseWheelHandler);
            Mouse.AddPreviewMouseDownHandler((DependencyObject)window, MouseDownHandler);
            this.connectedWindow = window;
        }

        void MouseWheelHandler(object sender, MouseWheelEventArgs mouseWheelEventArgs)
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

        protected virtual void OnWheelTilted()
        {
            WheelTilted?.Invoke(this, EventArgs.Empty);
        }
    }
}