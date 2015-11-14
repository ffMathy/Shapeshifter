using System.Diagnostics;
using System.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment
{
    internal class EnvironmentInformation : IEnvironmentInformation
    {
        public bool IsDebugging => Debugger.IsAttached;

        public bool IsInDesignTime => !(Application.Current is App);
    }
}