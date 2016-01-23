namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System;

    using Messages.Interfaces;

    public interface IMouseWheelHook
        : IWindowMessageInterceptor
    {
        event EventHandler WheelScrolledDown;

        event EventHandler WheelScrolledUp;

        event EventHandler WheelTilted;

        void ResetAccumulatedWheelDelta();
    }
}