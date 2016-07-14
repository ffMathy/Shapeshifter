namespace Shapeshifter.WindowsDesktop.Services.Keyboard.Interfaces
{
    using System.Threading.Tasks;
    using System.Windows.Input;

    public interface IKeyboardManager
    {
        bool IsKeyDown(Key key);

        Task SendKeysAsync(params Key[] keys);

        Task SendKeysAsync(params KeyOperation[] keyOperations);
    }
}