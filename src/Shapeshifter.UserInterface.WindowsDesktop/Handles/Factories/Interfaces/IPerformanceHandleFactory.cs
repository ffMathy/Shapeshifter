using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using System.Runtime.CompilerServices;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    public interface IPerformanceHandleFactory
    {
        IPerformanceHandle StartMeasuringPerformance([CallerMemberName] string methodName = "");
    }
}
