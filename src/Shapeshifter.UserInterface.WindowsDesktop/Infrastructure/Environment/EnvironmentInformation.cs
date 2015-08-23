using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System.Diagnostics;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment
{
    class EnvironmentInformation : IEnvironmentInformation
    {
        public bool IsDebugging
        {
            get
            {
                return Debugger.IsAttached;
            }
        }

        public bool IsInDesignTime
        {
            get
            {
                return !(Application.Current is App);
            }
        }
    }
}
