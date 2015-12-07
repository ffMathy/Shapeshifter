namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    using System.Runtime.CompilerServices;

    using Handles.Interfaces;

    public interface IPerformanceHandleFactory
    {
        IPerformanceHandle StartMeasuringPerformance([CallerMemberName] string methodName = "");
    }
}