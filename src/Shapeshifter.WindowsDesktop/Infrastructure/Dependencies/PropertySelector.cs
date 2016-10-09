using Autofac.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Dependencies
{
    class PropertySelector : IPropertySelector
    {
        public bool InjectProperty(PropertyInfo propertyInfo, object instance)
        {
            var attribute = propertyInfo.GetCustomAttribute<InjectAttribute>();
            if (attribute == null) return false;

            var value = propertyInfo.GetValue(instance);
            return value == null;
        }
    }
}
