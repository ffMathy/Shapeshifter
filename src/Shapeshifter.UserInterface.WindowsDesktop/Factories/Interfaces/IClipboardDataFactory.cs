using System.Runtime.InteropServices.ComTypes;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces
{
    interface IClipboardDataFactory
    {
        bool CanBuild(string format);

        IClipboardData Build(string format, object data);
    }
}
