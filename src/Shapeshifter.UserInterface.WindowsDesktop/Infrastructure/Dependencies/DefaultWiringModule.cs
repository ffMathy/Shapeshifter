using Autofac;
using Autofac.Builder;
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
                    IRegistrationBuilder<object, ReflectionActivatorData, object> registration;
                    if (type.IsGenericType)
                    {
                        registration = builder.RegisterGeneric(type);

                        var genericInterfaces = type
                            .GetInterfaces()
                            .Where(x => x.IsGenericType);
                        foreach(var genericInterface in genericInterfaces)
                        {
                            registration.As(genericInterface);
                        }
                    }
                    else
                    {
                        var standardRegistration = builder.RegisterType(type);
                        standardRegistration.AsSelf();
                        standardRegistration.AsImplementedInterfaces();

                        registration = standardRegistration;
                    }

                    registration.FindConstructorsWith(new PublicConstructorFinder());

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
