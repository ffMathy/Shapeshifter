namespace Shapeshifter.WindowsDesktop.Infrastructure.Signing.Interfaces
{
    using System.Security.Cryptography.X509Certificates;

    public interface ISignHelper
    {
        void SignAssemblyWithCertificate(string assemblyPath, X509Certificate2 certificate);
    }
}