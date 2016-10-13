namespace Shapeshifter.WindowsDesktop.Infrastructure.Environment
{
    using System;
    using System.Diagnostics;
    using System.Net.NetworkInformation;
    using System.Windows;

    using Interfaces;

    public class EnvironmentInformation: IEnvironmentInformation
    {
        readonly bool isInDesignTime;

        public EnvironmentInformation()
        {
            isInDesignTime = !(Application.Current is App);
        }

        public EnvironmentInformation(
            bool isInDesignTime)
        {
            this.isInDesignTime = isInDesignTime;
        }

        public bool GetIsDebugging()
        {
            return !GetShouldUpdate() || Debugger.IsAttached;
        }

        private bool IsRunningDeveloperVersion()
        {
            var assemblyVersion = typeof(IEnvironmentInformation)
                .Assembly
                .GetName()
                .Version;
            return assemblyVersion == new Version(1, 0, 0, 0);
        }

        public bool GetShouldUpdate()
        {
            return !IsRunningDeveloperVersion();
        }

        public bool GetHasInternetAccess()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return false;
            }

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var face in interfaces)
            {
                if (ProvidesInternetAccess(face))
                {
                    return true;
                }
            }

            return false;
        }

        static bool ProvidesInternetAccess(NetworkInterface face)
        {
            if ((face.OperationalStatus != OperationalStatus.Up) ||
                (face.NetworkInterfaceType == NetworkInterfaceType.Tunnel) ||
                (face.NetworkInterfaceType == NetworkInterfaceType.Loopback))
            {
                return false;
            }

            var statistics = face.GetIPv4Statistics();
            if ((statistics.BytesReceived > 0) && (statistics.BytesSent > 0))
            {
                return true;
            }
            return false;
        }

        public bool GetIsInDesignTime() => isInDesignTime;
    }
}