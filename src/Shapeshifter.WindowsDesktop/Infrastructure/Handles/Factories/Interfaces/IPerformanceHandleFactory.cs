namespace Shapeshifter.WindowsDesktop.Infrastructure.Handles.Factories.Interfaces
{
    using System.Runtime.CompilerServices;

    using Handles.Interfaces;

    public interface IPerformanceHandleFactory
    {
        IPerformanceHandle StartMeasuringPerformance([CallerMemberName] string methodName = "");
    }
}