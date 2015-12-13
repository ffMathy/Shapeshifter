namespace Shapeshifter.WindowsDesktop.Native.Interfaces
{
    using System;

    public interface IImageNativeApi
    {
        bool DeleteObject(IntPtr hObject);
    }
}