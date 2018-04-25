namespace Shapeshifter.WindowsDesktop.Services
{
	using System;
	using System.Threading.Tasks;
	using System.Windows.Input;

	using Infrastructure.Events;

	using Interfaces;

	using Messages;
	using Serilog;

	public class MouseWheelHook: IMouseWheelHook
    {
        readonly ILogger logger;

        int currentDelta;

        Message currentScrollTypeMessage;

        public event EventHandler WheelScrolledDown;
        public event EventHandler WheelScrolledUp;
        public event EventHandler WheelTilted;

        public bool IsConnected { get; private set; }

        public MouseWheelHook(
            ILogger logger)
        {
            this.logger = logger;
        }

        public void ResetAccumulatedWheelDelta()
        {
            currentDelta = 0;
        }

        void TriggerScrollEventsIfNeeded()
        {
            if ((currentDelta < Mouse.MouseWheelDeltaForOneLine) && 
                (currentDelta > -Mouse.MouseWheelDeltaForOneLine))
            {
                return;
            }

            switch (currentScrollTypeMessage) {

                case Message.WM_MOUSEHWHEEL:
                    OnWheelTilted();
                    break;

                case Message.WM_MOUSEWHEEL:
                    TriggerNeededEventsForCurrentDelta();
                    break;
            }

            ResetAccumulatedWheelDelta();
        }

        void TriggerNeededEventsForCurrentDelta()
        {
            if (currentDelta >= Mouse.MouseWheelDeltaForOneLine)
            {
                OnWheelScrolledUp();
            }
            else if (currentDelta <= -Mouse.MouseWheelDeltaForOneLine)
            {
                OnWheelScrolledDown();
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

        public Task ReceiveMessageEventAsync(
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

			return Task.CompletedTask;
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

            logger.Information("Current mouse wheel delta is " + currentDelta + ".");

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
            return (short) (e.WordParameter.ToInt64() >> 16);
        }
    }
}