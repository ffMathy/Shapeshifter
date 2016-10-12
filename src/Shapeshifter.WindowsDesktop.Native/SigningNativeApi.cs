namespace Shapeshifter.WindowsDesktop.Native
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    using Interfaces;

    [ExcludeFromCodeCoverage]
    public class SigningNativeApi : ISigningNativeApi
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SIGNER_SUBJECT_INFO
        {
            public uint cbSize;
            public IntPtr pdwIndex;
            public uint dwSubjectChoice;
            public SubjectChoiceUnion Union1;

            [StructLayout(LayoutKind.Explicit)]
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
            [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
            public struct SubjectChoiceUnion
            {
                [FieldOffset(0)]
                public IntPtr pSignerFileInfo;

                [FieldOffset(0)]
                public IntPtr pSignerBlobInfo;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIGNER_CERT
        {
            public uint cbSize;
            public uint dwCertChoice;
            public SignerCertUnion Union1;

            [StructLayout(LayoutKind.Explicit)]
            [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
            public struct SignerCertUnion
            {
                [FieldOffset(0)]
                public IntPtr pwszSpcFile;

                [FieldOffset(0)]
                public IntPtr pCertStoreInfo;

                [FieldOffset(0)]
                public IntPtr pSpcChainInfo;
            }

            public IntPtr hwnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIGNER_SIGNATURE_INFO
        {
            public uint cbSize;
            public uint algidHash; // ALG_ID
            public uint dwAttrChoice;
            public IntPtr pAttrAuthCode;
            public IntPtr psAuthenticated; // PCRYPT_ATTRIBUTES
            public IntPtr psUnauthenticated; // PCRYPT_ATTRIBUTES
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIGNER_FILE_INFO
        {
            public uint cbSize;
            public IntPtr pwszFileName;
            public IntPtr hFile;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIGNER_CERT_STORE_INFO
        {
            public uint cbSize;
            public IntPtr pSigningCert;
            public uint dwCertPolicy;
            public IntPtr hCertStore;
        }

        [DllImport("Mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SignerSign(
            IntPtr pSubjectInfo,
            IntPtr pSignerCert,
            IntPtr pSignatureInfo,
            IntPtr pProviderInfo,
            string pwszHttpTimeStamp,
            IntPtr psRequest,
            IntPtr pSipData);

        int ISigningNativeApi.SignerSign(
            IntPtr pSubjectInfo,
            IntPtr pSignerCert,
            IntPtr pSignatureInfo,
            IntPtr pProviderInfo,
            string pwszHttpTimeStamp,
            IntPtr psRequest,
            IntPtr pSipData)
        {
            return SignerSign(
                pSubjectInfo,
                pSignerCert,
                pSignatureInfo,
                pProviderInfo,
                pwszHttpTimeStamp,
                psRequest,
                pSipData);
        }

        [DllImport("Crypt32.dll", EntryPoint = "CertCreateCertificateContext", SetLastError = true, CharSet = CharSet.Unicode,
            ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CertCreateCertificateContext(
            int dwCertEncodingType,
            byte[] pbCertEncoded,
            int cbCertEncoded);

        IntPtr ISigningNativeApi.CertCreateCertificateContext(
            int dwCertEncodingType,
            byte[] pbCertEncoded,
            int cbCertEncoded)
        {
            return CertCreateCertificateContext(dwCertEncodingType, pbCertEncoded, cbCertEncoded);
        }
    }
}