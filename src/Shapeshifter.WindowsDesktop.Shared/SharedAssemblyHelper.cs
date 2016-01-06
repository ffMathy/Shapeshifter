namespace Shapeshifter.WindowsDesktop.Shared
{
    using System.Reflection;

    public static class SharedAssemblyHelper
    {
        public static Assembly Assembly => typeof (SharedAssemblyHelper).Assembly;
    }
}