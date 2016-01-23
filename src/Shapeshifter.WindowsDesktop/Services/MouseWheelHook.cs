namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Windows.Input;

    using Infrastructure.Events;

    using Interfaces;

    using Messages;

    public class MouseWheelHook: IMouseWheelHook
    {
        int currentDelta;

        Message currentScrollTypeMessage;

        public event EventHandler WheelScrolledDown;

        public event EventHandler WheelScrolledUp;

        public event EventHandler WheelTilted;

        public bool IsConnected { get; private set; }

        public void ResetAccumulatedWheelDelta()
        {
            currentDelta = 0;
        }

        void TriggerScrollEventsIfNeeded()
        {
            const int scrollAmountNeeded =
                Mouse.MouseWheelDeltaForOneLine;
            if (currentDelta > scrollAmountNeeded)
            {
                ResetAccumulatedWheelDelta();
                TriggerNeededEventsOnIncreasingDelta();
            }
            else if (currentDelta < -scrollAmountNeeded)
            {
                ResetAccumulatedWheelDelta();
                TriggerNeededEventsOnDecreasingDelta();
            }
        }

        void TriggerNeededEventsOnDecreasingDelta()
        {
            switch (currentScrollTypeMessage)
            {

                case Message.WM_MOUSEWHEEL:
                    OnWheelScrolledUp();
                    break;

                case Message.WM_MOUSEHWHEEL:
                    OnWheelTilted();
                    break;
            }
        }

        void TriggerNeededEventsOnIncreasingDelta()
        {
            switch (currentScrollTypeMessage) {

                case Message.WM_MOUSEWHEEL:
                    OnWheelScrolledDown();
                    break;

                case Message.WM_MOUSEHWHEEL:
                    OnWheelTilted();
                    break;
            }
        }

        bool GetIsSwitchingDirection(
            int delta,
            Message scrollTypeMessage)
        {
            return (scrollTypeMessage != currentScrollTypeMessage) ||
                   ((delta > 0) && (currentDelta < 0)) ||
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
            if (IsConnected)
            {
                throw new InvalidOperationException("Can't connect when already connected.");
            }

            IsConnected = true;
        }

        public void Uninstall()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Can't disconnect when already disconnected.");
            }

            IsConnected = false;
        }

        public void ReceiveMessageEvent(
            WindowMessageReceivedArgument e)
        {
            switch (e.Message)
            {
                case Message.WM_MOUSEWHEEL:
                case Message.WM_MOUSEHWHEEL:
                    HandleScrollingMessage(e);
                    break;

                case Message.WM_XBUTTONDOWN:
                    HandleExtraButtonMessage(e);
                    break;
            }
        }

        void HandleExtraButtonMessage(
            WindowMessageReceivedArgument e)
        {
            var buttonClicked = GetExtraButtonClickedFromWordParameter(e);

            const int XBUTTON1 = 0x0001;
            const int XBUTTON2 = 0x0002;
            if ((buttonClicked == XBUTTON1) || 
                (buttonClicked == XBUTTON2))
            {
                OnWheelTilted();
            }
        }

        void HandleScrollingMessage(WindowMessageReceivedArgument e)
        {
            var delta = GetDeltaFromWordParameter(e);
            ResetDeltaIfNewScrollDirection(e, delta);

            currentDelta += delta;
            currentScrollTypeMessage = e.Message;

            TriggerScrollEventsIfNeeded();
        }

        void ResetDeltaIfNewScrollDirection(WindowMessageReceivedArgument e, short delta)
        {
            var isSwitchingDirection = GetIsSwitchingDirection(
                delta,
                e.Message);
            if (isSwitchingDirection)
            {
                ResetAccumulatedWheelDelta();
            }
        }

        static short GetDeltaFromWordParameter(WindowMessageReceivedArgument e)
        {
            return GetHighOrderWord(e);
        }

        static short GetExtraButtonClickedFromWordParameter(WindowMessageReceivedArgument e)
        {
            return GetHighOrderWord(e);
        }

        static short GetHighOrderWord(WindowMessageReceivedArgument e)
        {
            return (short)(e.WordParameter.ToInt32() >> 16);
        }
    }
}