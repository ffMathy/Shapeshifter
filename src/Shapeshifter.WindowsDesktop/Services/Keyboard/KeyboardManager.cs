namespace Shapeshifter.WindowsDesktop.Services.Keyboard
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Native.Interfaces;

    using static Native.KeyboardNativeApi;

    class KeyboardManager: IKeyboardManager
    {
        readonly IKeyboardNativeApi nativeApi;
        readonly ILogger logger;

        public KeyboardManager(
            IKeyboardNativeApi nativeApi,
            ILogger logger)
        {
            this.nativeApi = nativeApi;
            this.logger = logger;
        }

        public bool IsKeyDown(Key key)
        {
            return Keyboard.IsKeyDown(key);
        }

        public async Task SendKeysAsync(params Key[] keys)
        {
            var operations = new LinkedList<KeyOperation>();
            foreach (var key in keys)
            {
                operations.AddLast(new KeyOperation(key, KeyDirection.Down));
                operations.AddLast(new KeyOperation(key, KeyDirection.Up));
            }

            await SendKeysAsync(operations.ToArray());
        }

        public async Task SendKeysAsync(params KeyOperation[] keyOperations)
        {
            var logString = keyOperations
                .Select(x => $"[{x.Key}: {x.Direction}]")
                .Aggregate((a, b) => $"{a}, {b}");
            logger.Information($"Sending key combinations {logString}.");

            var inputs = keyOperations
                .Select(
                    x => GenerateKeystoke(
                        MapKeyToVirtualKey(x.Key),
                        x.Direction == KeyDirection.Down ? 0 : KEYEVENTF.KEYUP))
                .ToArray();
            foreach (var input in inputs)
            {
                await Task.Delay(10);
                nativeApi.SendInput(
                    (uint)1,
                    new[]
                    {
                        input
                    },
                    INPUT.Size);
                await Task.Delay(10);
            }
        }

        static VirtualKeyShort MapKeyToVirtualKey(Key key)
        {
            return (VirtualKeyShort) KeyInterop.VirtualKeyFromKey(key);
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