namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    using System.Windows.Input;

    public interface IKeyboardManager
    {
        bool IsKeyDown(Key key);

        void SendKeyDown(Key key);

        void SendKeyUp(Key key);

        void SendKeys(params Key[] keys);

        void SendKeys(params KeyOperation[] keyOperations);
    }
}