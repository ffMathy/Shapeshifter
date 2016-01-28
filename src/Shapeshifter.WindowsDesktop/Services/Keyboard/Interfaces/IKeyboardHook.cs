namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    using System;

    using Infrastructure.Events;

    using Services.Interfaces;

    public interface IKeyboardHook: IHookService
    {
        event EventHandler<KeyDetectedArgument> KeyDetected;
    }
}