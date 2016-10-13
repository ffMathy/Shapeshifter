namespace Shapeshifter.WindowsDesktop.Native
{
    using System.Reflection;

    public static class NativeAssemblyHelper
    {
        public static Assembly Assembly => typeof (NativeAssemblyHelper).Assembly;
    }
}