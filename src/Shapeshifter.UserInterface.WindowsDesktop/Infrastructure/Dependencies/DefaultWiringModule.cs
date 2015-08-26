using Autofac;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System;
using System.Linq;
using System.Reflection;
using AutofacModule = Autofac.Module;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies
{
    public class DefaultWiringModule : AutofacModule
    {
        readonly Action<ContainerBuilder> callback;

        public DefaultWiringModule(Action<ContainerBuilder> callback = null)
        {
            this.callback = callback;
        }

        protected override void Load(ContainerBuilder builder)
        {
            RegisterAssemblyTypes(builder, typeof(DefaultWiringModule).Assembly);

            if (callback != null)
            {
                callback(builder);
            }

            base.Load(builder);
        }

        static void RegisterAssemblyTypes(ContainerBuilder builder, Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.IsClass && !type.IsAbstract)
                {
                    var registration = builder.RegisterType(type);
                    registration.FindConstructorsWith(new PublicConstructorFinder());
                    registration.AsSelf();
                    registration.AsImplementedInterfaces();

                    var interfaces = type.GetInterfaces();
                    if (interfaces.Contains(typeof(ISingleInstance)))
                    {
                        registration.SingleInstance();
                    }
                }
            }
        }
    }
}
