using System;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Dependencies
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class InjectAttribute: Attribute
    {
    }
}
