#region

using System;
using System.Runtime.InteropServices;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Api
{
    public static class GeneralApi
    {
        public const int GMEM_MOVABLE = 0x0002;
        public const int GMEM_ZEROINIT = 0x0040;

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = true)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        public static T ByteArrayToStructure<T>(byte[] data)
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

        public static byte[] StructureToByteArray<T>(T structure)
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