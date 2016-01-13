namespace Shapeshifter.WindowsDesktop.Native.Helpers.Interfaces
{
    using System.Security.Cryptography.X509Certificates;

    public interface ISignHelper
    {
        void SignAssemblyWithCertificate(string assemblyPath, X509Certificate2 certificate);
    }
}