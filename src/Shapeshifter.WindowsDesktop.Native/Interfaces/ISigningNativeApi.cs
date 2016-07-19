namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;

    public interface ISigningNativeApi {
        int SignerSign(IntPtr pSubjectInfo, IntPtr pSignerCert, IntPtr pSignatureInfo, IntPtr pProviderInfo, string pwszHttpTimeStamp, IntPtr psRequest, IntPtr pSipData);

        IntPtr CertCreateCertificateContext(int dwCertEncodingType, byte[] pbCertEncoded, int cbCertEncoded);
    }
}