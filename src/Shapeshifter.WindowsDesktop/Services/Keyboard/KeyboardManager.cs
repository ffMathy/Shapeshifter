namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using Interfaces;

    using Native.Interfaces;

    using static Native.KeyboardNativeApi;

    class KeyboardManager : IKeyboardManager
    {
        readonly IKeyboardNativeApi nativeApi;

        public KeyboardManager(
            IKeyboardNativeApi nativeApi)
        {
            this.nativeApi = nativeApi;
        }

        public bool IsKeyDown(Key key)
        {
            return Keyboard.IsKeyDown(key);
        }

        public void SendKeyDown(Key key)
        {
            SendKeys(new KeyOperation(key, KeyDirection.Down));
        }

        public void SendKeyUp(Key key)
        {
            SendKeys(new KeyOperation(key, KeyDirection.Up));
        }

        public void SendKeys(params Key[] keys)
        {
            var operations = new LinkedList<KeyOperation>();
            foreach (var key in keys)
            {
                operations.AddLast(new KeyOperation(key, KeyDirection.Down));
                operations.AddLast(new KeyOperation(key, KeyDirection.Up));
            }

            SendKeys(operations.ToArray());
        }

        public void SendKeys(params KeyOperation[] keyOperations)
        {
            var inputs = keyOperations
                .Select(
                    x => GenerateKeystoke(
                        MapKeyToVirtualKey(x.Key),
                        x.Direction == KeyDirection.Down ? 0 : KEYEVENTF.KEYUP))
                .ToArray();
            nativeApi.SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        static VirtualKeyShort MapKeyToVirtualKey(Key key)
        {
            return (VirtualKeyShort)KeyInterop.VirtualKeyFromKey(key);
        }

        static INPUT GenerateKeystoke(VirtualKeyShort key, KEYEVENTF flags = 0)
        {
            return new INPUT
            {
                type = 1,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = key,
                        dwFlags = flags,
                        wScan = 0
                    }
                }
            };
        }
    }
}
