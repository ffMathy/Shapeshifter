namespace Shapeshifter.WindowsDesktop.Native.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    using Interfaces;

    class SignHelper : ISignHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct SIGNER_SUBJECT_INFO
        {
            public uint cbSize;
            public IntPtr pdwIndex;
            public uint dwSubjectChoice;
            public SubjectChoiceUnion Union1;

            [StructLayout(LayoutKind.Explicit)]
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
            [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
            internal struct SubjectChoiceUnion
            {
                [FieldOffset(0)]
                public IntPtr pSignerFileInfo;
                [FieldOffset(0)]
                public IntPtr pSignerBlobInfo;
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SIGNER_CERT
        {
            public uint cbSize;
            public uint dwCertChoice;
            public SignerCertUnion Union1;

            [StructLayout(LayoutKind.Explicit)]
            [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
            internal struct SignerCertUnion
            {
                [FieldOffset(0)]
                public IntPtr pwszSpcFile;
                [FieldOffset(0)]
                public IntPtr pCertStoreInfo;
                [FieldOffset(0)]
                public IntPtr pSpcChainInfo;
            };

            public IntPtr hwnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SIGNER_SIGNATURE_INFO
        {
            public uint cbSize;
            public uint algidHash; // ALG_ID
            public uint dwAttrChoice;
            public IntPtr pAttrAuthCode;
            public IntPtr psAuthenticated; // PCRYPT_ATTRIBUTES
            public IntPtr psUnauthenticated; // PCRYPT_ATTRIBUTES
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SIGNER_FILE_INFO
        {
            public uint cbSize;
            public IntPtr pwszFileName;
            public IntPtr hFile;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SIGNER_CERT_STORE_INFO
        {
            public uint cbSize;
            public IntPtr pSigningCert;
            public uint dwCertPolicy;
            public IntPtr hCertStore;
        }

        [DllImport("Mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int SignerSign(
            IntPtr pSubjectInfo, 
            IntPtr pSignerCert,   
            IntPtr pSignatureInfo,   
            IntPtr pProviderInfo,      
            string pwszHttpTimeStamp,
            IntPtr psRequest,   
            IntPtr pSipData);

        [DllImport("Crypt32.dll", EntryPoint = "CertCreateCertificateContext", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr CertCreateCertificateContext(
            int dwCertEncodingType,
            byte[] pbCertEncoded,
            int cbCertEncoded);
        
        public void SignAssemblyWithCertificate(string assemblyPath, X509Certificate2 certificate)
        {
            var pSignerCert = IntPtr.Zero;
            var pSubjectInfo = IntPtr.Zero;
            var pSignatureInfo = IntPtr.Zero;
            var pProviderInfo = IntPtr.Zero;

            try
            {
                pSignerCert = CreateSignerCertificate(certificate);
                pSubjectInfo = CreateSignerSubjectInfo(assemblyPath);
                pSignatureInfo = CreateSignerSignatureInfo();

                SignCode(pSubjectInfo, pSignerCert, pSignatureInfo, pProviderInfo);
            }
            catch (CryptographicException ce)
            {
                throw new CryptographicException($@"An error occurred while attempting to load the signing certificate. {ce.Message}", ce);
            }
            finally
            {
                if (pSignerCert != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(pSignerCert, typeof(SIGNER_CERT));
                }
                if (pSubjectInfo != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(pSubjectInfo, typeof(SIGNER_SUBJECT_INFO));
                }
                if (pSignatureInfo != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(pSignatureInfo, typeof(SIGNER_SIGNATURE_INFO));
                }
            }
        }

        static IntPtr CreateSignerSubjectInfo(string pathToAssembly)
        {
            var info = new SIGNER_SUBJECT_INFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_SUBJECT_INFO)),
                pdwIndex = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint)))
            };

            const int index = 0;
            Marshal.StructureToPtr(index, info.pdwIndex, false);

            info.dwSubjectChoice = 0x1; //SIGNER_SUBJECT_FILE
            var assemblyFilePtr = Marshal.StringToHGlobalUni(pathToAssembly);

            var fileInfo = new SIGNER_FILE_INFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_FILE_INFO)),
                pwszFileName = assemblyFilePtr,
                hFile = IntPtr.Zero
            };

            info.Union1 = new SIGNER_SUBJECT_INFO.SubjectChoiceUnion
            {
                pSignerFileInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SIGNER_FILE_INFO)))
            };

            Marshal.StructureToPtr(fileInfo, info.Union1.pSignerFileInfo, false);

            var pSubjectInfo = Marshal.AllocHGlobal(Marshal.SizeOf(info));
            Marshal.StructureToPtr(info, pSubjectInfo, false);

            return pSubjectInfo;
        }

        static IntPtr CreateSignerCertificate(X509Certificate2 certificate)
        {
            var signerCertificate = new SIGNER_CERT
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_CERT)),
                dwCertChoice = 0x2,
                Union1 = new SIGNER_CERT.SignerCertUnion
                {
                    pCertStoreInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SIGNER_CERT_STORE_INFO)))
                },
                hwnd = IntPtr.Zero
            };

            const int X509_ASN_ENCODING = 0x00000001;
            const int PKCS_7_ASN_ENCODING = 0x00010000;

            var pCertContext = CertCreateCertificateContext(
                X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                certificate.GetRawCertData(),
                certificate.GetRawCertData().Length);

            var certStoreInfo = new SIGNER_CERT_STORE_INFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_CERT_STORE_INFO)),
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
            var signatureInfo = new SIGNER_SIGNATURE_INFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SIGNER_SIGNATURE_INFO)),
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
        
        static void SignCode(IntPtr pSubjectInfo, IntPtr pSignerCert, IntPtr pSignatureInfo, IntPtr pProviderInfo)
        {
            var hResult = SignerSign(
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
