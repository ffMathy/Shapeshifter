using System.Runtime.CompilerServices;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    public interface IPerformanceHandleFactory
    {
        IPerformanceHandle StartMeasuringPerformance([CallerMemberName] string methodName = "");
    }
}