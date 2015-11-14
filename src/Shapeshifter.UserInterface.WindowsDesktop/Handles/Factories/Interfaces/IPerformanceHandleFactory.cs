#region

using System.Runtime.CompilerServices;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    public interface IPerformanceHandleFactory
    {
        IPerformanceHandle StartMeasuringPerformance([CallerMemberName] string methodName = "");
    }
}