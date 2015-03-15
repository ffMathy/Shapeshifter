using Autofac;
using System.Collections.Generic;
using System.Reflection;

namespace Shapeshifter.Core.Helpers
{
    public static class InversionOfControlHelper
    {

        private static List<Assembly> additionalAssemblies;

        private static IContainer container;
        public static IContainer Container
        {
            get
            {
                if (container == null)
                {
                    var builder = new ContainerBuilder();

                    var executingAssembly = Assembly.GetExecutingAssembly();
                    var callingAssembly = Assembly.GetCallingAssembly();
                    var entryAssembly = Assembly.GetEntryAssembly();

                    var assemblies = new List<Assembly>();
                    if (executingAssembly != null) assemblies.Add(executingAssembly);
                    if (callingAssembly != null) assemblies.Add(callingAssembly);
                    if (entryAssembly != null) assemblies.Add(entryAssembly);

                    assemblies.AddRange(additionalAssemblies);

                    builder
                        .RegisterAssemblyTypes(assemblies.ToArray())
                        .AsImplementedInterfaces();

                    container = builder.Build();
                }
                return container;
            }
        }

        static InversionOfControlHelper()
        {
            additionalAssemblies = new List<Assembly>();
        }

        public static void InjectAssemblies(params Assembly[] assemblies)
        {
            additionalAssemblies.AddRange(assemblies);
        }
    }
}
