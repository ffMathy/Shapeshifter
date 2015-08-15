using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    [ExcludeFromCodeCoverage]
    class KeyEventArgument
    {
        /// <summary>
        /// The key pressed.
        /// </summary>
        public readonly Key Key;

        /// <summary>
        /// Create raw keyevent arguments.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="isSystemKey"></param>
        public KeyEventArgument(int keyCode)
        {
            Key = KeyInterop.KeyFromVirtualKey(keyCode);
        }
    }
}
