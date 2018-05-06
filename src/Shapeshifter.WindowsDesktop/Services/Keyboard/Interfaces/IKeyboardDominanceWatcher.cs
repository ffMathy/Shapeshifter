namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    using Infrastructure.Dependencies.Interfaces;

    public interface IKeyboardDominanceWatcher: ISingleInstance
    {
        void Start();
        void Stop();
    }
}