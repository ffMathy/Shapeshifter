namespace Shapeshifter.WindowsDesktop.Infrastructure.Signing
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    using Interfaces;

    using Logging.Interfaces;

    using Native;
    using Native.Interfaces;

    class SignHelper: ISignHelper
    {
        readonly ISigningNativeApi signingNativeApi;

        public ILogger Logger { get; set; }

        public SignHelper(
            ISigningNativeApi signingNativeApi)
        {
            this.signingNativeApi = signingNativeApi;
        }

        public void SignAssemblyWithCertificate(string assemblyPath, X509Certificate2 certificate)
        {
            var pSignerCert = IntPtr.Zero;
            var pSubjectInfo = IntPtr.Zero;
            var pSignatureInfo = IntPtr.Zero;
            var pProviderInfo = IntPtr.Zero;

            try
            {
                pSignerCert = CreateSignerCertificate(certificate);
                Logger.Information("Signer certificate for given code signing certificate created.");

                pSubjectInfo = CreateSignerSubjectInfo(assemblyPath);
                Logger.Information("Signer signer subject information for given code signing certificate created.");

                pSignatureInfo = CreateSignerSignatureInfo();
                Logger.Information("Signer signer signature information for given code signing certificate created.");

                SignCode(pSubjectInfo, pSignerCert, pSignatureInfo, pProviderInfo);
                Logger.Information("Code has been successfully signed.");
            }
            catch (CryptographicException ce)
            {
                Logger.Error(ce);
                throw new CryptographicException($@"An error occurred while attempting to load the signing certificate. {ce.Message}", ce);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
            finally
            {
                if (pSignerCert != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(pSignerCert, typeof (SigningNativeApi.SIGNER_CERT));
                }
                if (pSubjectInfo != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(pSubjectInfo, typeof (SigningNativeApi.SIGNER_SUBJECT_INFO));
                }
                if (pSignatureInfo != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(pSignatureInfo, typeof (SigningNativeApi.SIGNER_SIGNATURE_INFO));
                }
                Logger.Information("Done signing assembly.");
            }
        }

        static IntPtr CreateSignerSubjectInfo(string pathToAssembly)
        {
            var info = new SigningNativeApi.SIGNER_SUBJECT_INFO
            {
                cbSize = (uint) Marshal.SizeOf(typeof (SigningNativeApi.SIGNER_SUBJECT_INFO)),
                pdwIndex = Marshal.AllocHGlobal(Marshal.SizeOf(typeof (uint)))
            };

            const int index = 0;
            Marshal.StructureToPtr(index, info.pdwIndex, false);

            const uint SIGNER_SUBJECT_FILE = 0x1;
            info.dwSubjectChoice = SIGNER_SUBJECT_FILE;
            var assemblyFilePtr = Marshal.StringToHGlobalUni(pathToAssembly);

            var fileInfo = new SigningNativeApi.SIGNER_FILE_INFO
            {
                cbSize = (uint) Marshal.SizeOf(typeof (SigningNativeApi.SIGNER_FILE_INFO)),
                pwszFileName = assemblyFilePtr,
                hFile = IntPtr.Zero
            };

            info.Union1 = new SigningNativeApi.SIGNER_SUBJECT_INFO.SubjectChoiceUnion
            {
                pSignerFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof (SigningNativeApi.SIGNER_FILE_INFO)))
            };

            Marshal.StructureToPtr(fileInfo, info.Union1.pSignerFileInfo, false);

            var pSubjectInfo = Marshal.AllocHGlobal(Marshal.SizeOf(info));
            Marshal.StructureToPtr(info, pSubjectInfo, false);

            return pSubjectInfo;
        }

        IntPtr CreateSignerCertificate(X509Certificate2 certificate)
        {
            var signerCertificate = new SigningNativeApi.SIGNER_CERT
            {
                cbSize = (uint) Marshal.SizeOf(typeof (SigningNativeApi.SIGNER_CERT)),
                dwCertChoice = 0x2,
                Union1 = new SigningNativeApi.SIGNER_CERT.SignerCertUnion
                {
                    pCertStoreInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof (SigningNativeApi.SIGNER_CERT_STORE_INFO)))
                },
                hwnd = IntPtr.Zero
            };

            const int X509_ASN_ENCODING = 0x00000001;
            const int PKCS_7_ASN_ENCODING = 0x00010000;

            var pCertContext = signingNativeApi.CertCreateCertificateContext(
                X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                certificate.GetRawCertData(),
                certificate.GetRawCertData()
                           .Length);

            var certStoreInfo = new SigningNativeApi.SIGNER_CERT_STORE_INFO
            {
                cbSize = (uint) Marshal.SizeOf(typeof (SigningNativeApi.SIGNER_CERT_STORE_INFO)),
                pSigningCert = pCertContext,
                dwCertPolicy = 0x2,
                hCertStore = IntPtr.Zero
            };

            Marshal.StructureToPtr(certStoreInfo, signerCertificate.Union1.pCertStoreInfo, false);

            var pSignerCert = Marshal.AllocHGlobal(Marshal.SizeOf(signerCertificate));
            Marshal.StructureToPtr(signerCertificate, pSignerCert, false);

            return pSignerCert;
        }

        static IntPtr CreateSignerSignatureInfo()
        {
            var signatureInfo = new SigningNativeApi.SIGNER_SIGNATURE_INFO
            {
                cbSize = (uint) Marshal.SizeOf(typeof (SigningNativeApi.SIGNER_SIGNATURE_INFO)),
                algidHash = 0x00008004,
                dwAttrChoice = 0x0,
                pAttrAuthCode = IntPtr.Zero,
                psAuthenticated = IntPtr.Zero,
                psUnauthenticated = IntPtr.Zero
            };

            var pSignatureInfo = Marshal.AllocHGlobal(Marshal.SizeOf(signatureInfo));
            Marshal.StructureToPtr(signatureInfo, pSignatureInfo, false);

            return pSignatureInfo;
        }

        void SignCode(IntPtr pSubjectInfo, IntPtr pSignerCert, IntPtr pSignatureInfo, IntPtr pProviderInfo)
        {
            var hResult = signingNativeApi.SignerSign(
                pSubjectInfo,
                pSignerCert,
                pSignatureInfo,
                pProviderInfo,
                null,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hResult != 0)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }
}