namespace Shapeshifter.WindowsDesktop.Infrastructure.Environment
{
    using System.Diagnostics;
    using System.Windows;

    using Interfaces;

    class EnvironmentInformation: IEnvironmentInformation
    {
        public bool IsDebugging => Debugger.IsAttached;

        public bool IsInDesignTime => !(Application.Current is App);
    }
}