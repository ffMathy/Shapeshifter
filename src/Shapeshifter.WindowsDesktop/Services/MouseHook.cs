namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    using Controls.Window.Interfaces;

    using Interfaces;

    public class MouseWheelHook: IMouseWheelHook
    {
        IHookableWindow connectedWindow;

        int currentDelta;

        public event EventHandler WheelScrolledDown;

        public event EventHandler WheelScrolledUp;

        public event EventHandler WheelTilted;

        public bool IsConnected
            => connectedWindow != null;

        public void ResetAccumulatedWheelDelta()
        {
            currentDelta = 0;
        }

        public void Disconnect()
        {
            Mouse.RemovePreviewMouseWheelHandler((DependencyObject) connectedWindow, MouseWheelHandler);
            Mouse.RemovePreviewMouseDownHandler((DependencyObject) connectedWindow, MouseDownHandler);

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

        public void Connect(
            IHookableWindow window)
        {
            var dependencyObject = (DependencyObject) window;
            Mouse.AddPreviewMouseWheelHandler(
                dependencyObject, MouseWheelHandler);
            Mouse.AddPreviewMouseDownHandler(
                dependencyObject, MouseDownHandler);

            connectedWindow = window;
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

        void CheckForSwitchingDirections(int delta)
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

        bool GetIsSwitchingDirection(int delta)
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
    }
}