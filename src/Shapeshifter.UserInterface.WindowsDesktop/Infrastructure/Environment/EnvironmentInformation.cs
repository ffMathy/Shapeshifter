using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System;
using System.Diagnostics;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment
{
    class EnvironmentInformation : IEnvironmentInformation
    {
        public bool IsDebugging
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsInDesignTime
        {
            get
            {
                return Debugger.IsAttached;
            }
        }
    }
}
