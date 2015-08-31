using Autofac;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers
{
    static class DesignTimeContainerHelper
    {
        public static IContainer CreateDesignTimeContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DefaultWiringModule());

            RegisterFakes(builder);

            return builder.Build();
        }

        public static void RegisterFakes(ContainerBuilder builder)
        {
            SubstituteFake<IUpdateService>(builder);
            SubstituteFake<IFileManager>(builder);
            SubstituteFake<IProcessManager>(builder);
            SubstituteFake<IDomainNameResolver>(builder);
        }

        static void SubstituteFake<TInterface>(ContainerBuilder builder) where TInterface : class
        {
            builder
                .RegisterInstance((TInterface)Substitute.For<TInterface>())
                .As<TInterface>()
                .SingleInstance();
        }
    }
}
