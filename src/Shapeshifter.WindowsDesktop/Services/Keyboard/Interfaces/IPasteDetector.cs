namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    using System;

    public interface IPasteDetector
    {
        event EventHandler PasteDetected;
    }
}