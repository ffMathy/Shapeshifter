namespace Shapeshifter.WindowsDesktop.Native
{
    using System;
    using System.Runtime.InteropServices;

    using Interfaces;

    public class GeneralNativeApi: IGeneralNativeApi
    {
        public const int GMEM_MOVABLE = 0x0002;

        public const int GMEM_ZEROINIT = 0x0040;

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmGetColorizationColor(out uint ColorizationColor, [MarshalAs(UnmanagedType.Bool)]out bool ColorizationOpaqueBlend);

        void IGeneralNativeApi.DwmGetColorizationColor(
            out uint ColorizationColor,
            [MarshalAs(UnmanagedType.Bool)] out bool ColorizationOpaqueBlend)
        {
            DwmGetColorizationColor(
                out ColorizationColor,
                out ColorizationOpaqueBlend);
        }

        void IGeneralNativeApi.CopyMemory(IntPtr dest, IntPtr src, uint count)
        {
            CopyMemory(dest, src, count);
        }

        IntPtr IGeneralNativeApi.GlobalAlloc(uint uFlags, UIntPtr dwBytes)
        {
            return GlobalAlloc(uFlags, dwBytes);
        }

        IntPtr IGeneralNativeApi.GlobalFree(IntPtr hMem)
        {
            return GlobalFree(hMem);
        }

        IntPtr IGeneralNativeApi.GlobalLock(IntPtr hMem)
        {
            return GlobalLock(hMem);
        }

        UIntPtr IGeneralNativeApi.GlobalSize(IntPtr hMem)
        {
            return GlobalSize(hMem);
        }

        bool IGeneralNativeApi.GlobalUnlock(IntPtr hMem)
        {
            return GlobalUnlock(hMem);
        }

        byte[] IGeneralNativeApi.StructureToByteArray<T>(T structure)
        {
            return StructureToByteArray(structure);
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = true)]
        internal static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock(IntPtr hMem);

        public T ByteArrayToStructure<T>(byte[] data)
        {
            var size = Marshal.SizeOf(typeof (T));

            var pointer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, 0, pointer, size);

                return (T) Marshal.PtrToStructure(pointer, typeof (T));
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }

        internal static byte[] StructureToByteArray<T>(T structure)
        {
            var size = Marshal.SizeOf(structure);
            var buffer = new byte[size];

            var pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structure, pointer, true);
            try
            {
                Marshal.Copy(pointer, buffer, 0, size);
                return buffer;
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }
    }
}