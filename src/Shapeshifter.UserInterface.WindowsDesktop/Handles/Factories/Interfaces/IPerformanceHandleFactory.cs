using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces
{
    public interface IPerformanceHandleFactory
    {
        IPerformanceHandle StartMeasuringPerformance([CallerMemberName] string methodName = "");
    }
}
