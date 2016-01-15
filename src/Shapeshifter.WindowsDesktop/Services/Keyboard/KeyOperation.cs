namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System.Windows.Input;

    public struct KeyOperation
    {
        public KeyOperation(Key key, KeyDirection direction)
        {
            Key = key;
            Direction = direction;
        }

        public Key Key { get; set; }

        public KeyDirection Direction { get; set; }
    }
}