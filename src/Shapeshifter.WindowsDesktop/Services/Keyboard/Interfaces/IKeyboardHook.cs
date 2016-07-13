namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    using System;

    using Infrastructure.Events;

    using Services.Interfaces;
    using System.Collections.Generic;
    using System.Windows.Input;

    public interface IKeyboardHook: IHookService
    {
        event EventHandler<KeyDetectedArgument> KeyDetected;

        ICollection<Key> IgnoredKeys { get; }
    }
}