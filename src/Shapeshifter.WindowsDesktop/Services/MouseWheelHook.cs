namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Infrastructure.Events;

    using Interfaces;

    using Messages;

    public class MouseWheelHook: IMouseWheelHook
    {
        IClipboardListWindow mainWindow;

        int currentDelta;

        public event EventHandler WheelScrolledDown;

        public event EventHandler WheelScrolledUp;

        public event EventHandler WheelTilted;

        public bool IsConnected { get; private set; }

        public MouseWheelHook(
            IClipboardListWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void ResetAccumulatedWheelDelta()
        {
            currentDelta = 0;
        }

        void MouseDownHandler(
            object sender,
            MouseButtonEventArgs e)
        {
            if ((e.XButton1 == MouseButtonState.Pressed) ||
                (e.XButton2 == MouseButtonState.Pressed))
            {
                OnWheelTilted();
            }
        }

        void MouseWheelHandler(
            object sender,
            MouseWheelEventArgs mouseWheelEventArgs)
        {
            var delta = mouseWheelEventArgs.Delta;
            CheckForSwitchingDirections(delta);

            currentDelta += delta;

            TriggerScrollEventsIfNeeded();
        }

        void CheckForSwitchingDirections(
            int delta)
        {
            var isSwitchingDirection = GetIsSwitchingDirection(delta);
            if (isSwitchingDirection)
            {
                ResetAccumulatedWheelDelta();
            }
        }

        void TriggerScrollEventsIfNeeded()
        {
            const int scrollAmountNeeded =
                Mouse.MouseWheelDeltaForOneLine;
            if (currentDelta > scrollAmountNeeded)
            {
                ResetAccumulatedWheelDelta();
                OnWheelScrolledDown();
            }
            else if (currentDelta < -scrollAmountNeeded)
            {
                ResetAccumulatedWheelDelta();
                OnWheelScrolledUp();
            }
        }

        bool GetIsSwitchingDirection(
            int delta)
        {
            return ((delta > 0) && (currentDelta < 0)) ||
                   ((delta < 0) && (currentDelta > 0));
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

        public void Install(
            IntPtr windowHandle)
        {
            var dependencyObject = (DependencyObject) mainWindow;
            Mouse.AddPreviewMouseWheelHandler(
                dependencyObject,
                MouseWheelHandler);
            Mouse.AddPreviewMouseDownHandler(
                dependencyObject,
                MouseDownHandler);

            IsConnected = true;
        }

        public void Uninstall()
        {
            var dependencyObject = (DependencyObject) mainWindow;
            Mouse.RemovePreviewMouseWheelHandler(
                dependencyObject,
                MouseWheelHandler);
            Mouse.RemovePreviewMouseDownHandler(
                dependencyObject,
                MouseDownHandler);

            mainWindow = null;
        }

        public void ReceiveMessageEvent(
            WindowMessageReceivedArgument e)
        {
            if ((e.Message == Message.WM_MOUSEWHEEL) ||
                (e.Message == Message.WM_MOUSEHWHEEL))
            {
                
            }
        }
    }
}