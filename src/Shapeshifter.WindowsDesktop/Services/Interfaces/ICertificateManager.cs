namespace Shapeshifter.WindowsDesktop.Services.Interfaces
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    using Org.BouncyCastle.Crypto;

    public interface ICertificateManager
    {
        /// <param name="issuerName"></param>
        /// <param name="issuerPrivateKey">The private key for the self signed certificate.</param>
        /// <param name="subjectName"></param>
        /// <returns>Returns the self signed certificate.</returns>
        X509Certificate2 GenerateSelfSignedCertificate(
            string subjectName,
            string issuerName,
            AsymmetricKeyParameter issuerPrivateKey);

        void InstallCertificateToStore(
            X509Certificate2 certificate,
            StoreName storeName,
            StoreLocation storeLocation);

        IReadOnlyCollection<X509Certificate2> GetCertificatesByIssuerFromStore(
            string issuer,
            StoreName storeName,
            StoreLocation storeLocation);
        
        /// <returns>The private key for the CA certificate.</returns>
        AsymmetricKeyParameter GenerateCACertificate(string subjectName);
    }
}