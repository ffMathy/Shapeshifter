namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System;

    public interface IMouseWheelHook: IHookService
    {
        event EventHandler WheelScrolledDown;

        event EventHandler WheelScrolledUp;

        event EventHandler WheelTilted;
    }
}