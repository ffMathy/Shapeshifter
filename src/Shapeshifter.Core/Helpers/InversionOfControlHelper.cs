using Autofac;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Shapeshifter.Core.Helpers
{
    public static class InversionOfControlHelper
    {
        
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

                    //automatically import all assemblies by analyzing the call stack.
                    var stackTrace = new StackTrace();
                    foreach(var frame in stackTrace.GetFrames())
                    {
                        var method = frame.GetMethod();
                        var type = method.DeclaringType;
                        var assembly = type.Assembly;

                        if(!assemblies.Contains(assembly) && assembly.FullName.StartsWith("Shapeshifter"))
                        {
                            assemblies.Add(assembly);
                        }
                    }

                    //now register everything.
                    builder
                        .RegisterAssemblyTypes(assemblies.ToArray())
                        .AsImplementedInterfaces();

                    container = builder.Build();
                }
                return container;
            }
        }
    }
}
