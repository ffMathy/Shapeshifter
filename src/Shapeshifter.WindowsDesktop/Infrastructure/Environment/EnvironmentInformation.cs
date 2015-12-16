namespace Shapeshifter.WindowsDesktop.Infrastructure.Environment
{
    using System.Diagnostics;
    using System.Windows;

    using Interfaces;

    class EnvironmentInformation: IEnvironmentInformation
    {
        public EnvironmentInformation()
        {
            IsInDesignTime = !(Application.Current is App);
        }

        public EnvironmentInformation(
            bool isInDesignTime)
        {
            this.IsInDesignTime = isInDesignTime;
        }

        public bool IsDebugging => Debugger.IsAttached;

        public bool IsInDesignTime { get; }
    }
}