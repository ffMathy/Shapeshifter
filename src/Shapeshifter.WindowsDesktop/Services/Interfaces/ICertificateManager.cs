namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    public interface ICertificateManager
    {
        /// <returns>Returns the self signed certificate.</returns>
        X509Certificate2 GenerateSelfSignedCertificate(
            string name);

        void InstallCertificateToStore(
            X509Certificate2 certificate,
            StoreName storeName,
            StoreLocation storeLocation);

        IReadOnlyCollection<X509Certificate2> GetCertificatesByIssuerFromStore(
            string issuer,
            StoreName storeName,
            StoreLocation storeLocation);
    }
}