namespace Shapeshifter.WindowsDesktop.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    using Interfaces;

    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.Pkcs;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Operators;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Crypto.Prng;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Pkcs;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.Utilities;
    using Org.BouncyCastle.X509;

    public class CertificateManager: ICertificateManager
    {
        const int ExpireTimeInYears = 30;
        const int KeyStrength = 2048;

        public X509Certificate2 GenerateSelfSignedCertificate(string name)
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            var certificateGenerator = new X509V3CertificateGenerator();

            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            var subjectDN = new X509Name(name);
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(ExpireTimeInYears);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            var keyGenerationParameters = new KeyGenerationParameters(random, KeyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            var issuerKeyPair = subjectKeyPair;

            var signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerKeyPair.Private, random);
            var certificate = certificateGenerator.Generate(signatureFactory);

            var info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);

            var x509 = new X509Certificate2(certificate.GetEncoded());

            var privateKey = info.ParsePrivateKey();
            var sequence = (Asn1Sequence) Asn1Object.FromByteArray(privateKey.GetDerEncoded());
            if (sequence.Count != 9)
            {
                throw new PemException("Malformed sequence in RSA private key.");
            }
            
            var rsa = RsaPrivateKeyStructure.GetInstance(sequence);
            var rsaParameters = new RsaPrivateCrtKeyParameters(
                rsa.Modulus,
                rsa.PublicExponent,
                rsa.PrivateExponent,
                rsa.Prime1,
                rsa.Prime2,
                rsa.Exponent1,
                rsa.Exponent2,
                rsa.Coefficient);

            var privateKeyRSA = DotNetUtilities.ToRSA(rsaParameters);

            var csp = new CspParameters
            {
                KeyContainerName = "Shapeshifter"
            };

            var rsaPrivate = new RSACryptoServiceProvider(csp);
            rsaPrivate.ImportParameters(privateKeyRSA.ExportParameters(true));
            x509.PrivateKey = rsaPrivate;

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

        public IReadOnlyCollection<X509Certificate2> GetCertificatesByIssuerFromStore(
            string issuer,
            StoreName storeName,
            StoreLocation storeLocation)
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
    }
}