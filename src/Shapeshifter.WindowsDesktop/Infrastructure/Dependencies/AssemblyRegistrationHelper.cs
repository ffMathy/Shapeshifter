namespace Shapeshifter.WindowsDesktop.Infrastructure.Dependencies
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Autofac;
    using Autofac.Builder;

    using Controls.Designer.Services;

    using Interfaces;

    public static class AssemblyRegistrationHelper
    {

        public static void RegisterAssemblyTypes(
            ContainerBuilder builder, 
            Assembly assembly,
            bool isInDesignerMode)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                var interfaces = type.GetInterfaces();
                if (interfaces.Contains(typeof(IDesignerService)) && !isInDesignerMode)
                {
                    continue;
                }

                IRegistrationBuilder<object, ReflectionActivatorData, object> registration;
                if (type.IsGenericType)
                {
                    registration = builder.RegisterGeneric(type);

                    var genericInterfaces = interfaces
                        .Where(x => x.IsGenericType);
                    foreach (var genericInterface in genericInterfaces)
                    {
                        registration = registration.As(genericInterface);
                    }
                }
                else
                {
                    var standardRegistration = builder.RegisterType(type);
                    standardRegistration.AsSelf();
                    standardRegistration.AsImplementedInterfaces();

                    registration = standardRegistration;
                }

                registration = registration.FindConstructorsWith(new PublicConstructorFinder());

                if (interfaces.Contains(typeof(ISingleInstance)))
                {
                    registration.SingleInstance();
                }
            }
        }
    }
}
