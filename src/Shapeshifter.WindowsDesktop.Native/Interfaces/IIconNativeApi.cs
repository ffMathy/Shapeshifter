namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;
    using System.Runtime.InteropServices;

    public interface IIconNativeApi
    {
        bool DeleteObject(IntPtr hObject);

        int GetObject(IntPtr hgdiobj, int cbBuffer, out IconNativeApi.BITMAP lpvObject);

        void SHCreateItemFromParsingName(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] [Out] out IconNativeApi.IShellItem ppv);

        IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref IconNativeApi.SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
    }
}