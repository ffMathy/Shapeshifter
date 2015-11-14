#region

using System.Diagnostics;
using System.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment
{
    internal class EnvironmentInformation : IEnvironmentInformation
    {
        public bool IsDebugging
        {
            get { return Debugger.IsAttached; }
        }

        public bool IsInDesignTime
        {
            get { return !(Application.Current is App); }
        }
    }
}