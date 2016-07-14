namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    using Services.Interfaces;

    public interface IPasteDetectionHandler:
        IHookService, 
        IPasteDetector
    {

    }
}