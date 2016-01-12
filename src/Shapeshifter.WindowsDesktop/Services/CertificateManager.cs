namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Windows;

    using Interfaces;

    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.Pkcs;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Crypto.Prng;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Pkcs;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.Utilities;
    using Org.BouncyCastle.X509;

    public class CertificateManager : ICertificateManager
    {
        public X509Certificate2 GenerateSelfSignedCertificate(
            string subjectName,
            string issuerName,
            AsymmetricKeyParameter issuerPrivateKey)
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            const string signatureAlgorithm = "SHA256WithRSA";
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

            var subjectDN = new X509Name(subjectName);
            var issuerDN = new X509Name(issuerName);
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            const int expireTimeInYears = 10;

            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(expireTimeInYears);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            const int keyStrength = 2048;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var certificate = certificateGenerator.Generate(issuerPrivateKey, random);

            var privateKeyInformation = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);

            var x509 = new X509Certificate2(certificate.GetEncoded());

            var sequence = (Asn1Sequence)Asn1Object.FromByteArray(privateKeyInformation.PrivateKey.GetDerEncoded());
            if (sequence.Count != 9)
                throw new PemException("Malformed sequence in RSA private key.");

            var rsa = new RsaPrivateKeyStructure(sequence);
            var rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);

            return x509;

        }

        public void InstallCertificateToStore(
            X509Certificate2 certificate,
            StoreName storeName,
            StoreLocation storeLocation)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    store.Add(certificate);
                }
                finally
                {
                    store.Close();
                }
            }
        }

        public IReadOnlyCollection<X509Certificate2> GetCertificatesByIssuerFromStore(string issuer, StoreName storeName, StoreLocation storeLocation)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                try
                {
                    return store
                        .Certificates
                        .OfType<X509Certificate2>()
                        .Where(x => x.Issuer == issuer)
                        .ToArray();
                }
                finally
                {
                    store.Close();
                }
            }
        }

        public AsymmetricKeyParameter GenerateCACertificate(string subjectName, int keyStrength = 2048)
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            const string signatureAlgorithm = "SHA256WithRSA";
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

            var subjectDN = new X509Name(subjectName);
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;

            var certificate = certificateGenerator.Generate(issuerKeyPair.Private, random);
            var certificate2 = new X509Certificate2(certificate.GetEncoded());

            InstallCertificateToStore(
                certificate2,
                StoreName.Root,
                StoreLocation.LocalMachine);

            return issuerKeyPair.Private;

        }
    }
}