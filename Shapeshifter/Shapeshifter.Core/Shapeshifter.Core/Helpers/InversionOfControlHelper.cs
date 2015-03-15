using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Helpers
{
    internal static class InversionOfControlHelper
    {
        private static ILifetimeScope container;
        public static ILifetimeScope Container
        {
            get
            {
                if(container == null)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var builder = new ContainerBuilder();

                    builder
                        .RegisterAssemblyTypes(assembly)
                        .AsImplementedInterfaces();

                    container = builder.Build();
                }
                return container;
            }
        }
    }
}
