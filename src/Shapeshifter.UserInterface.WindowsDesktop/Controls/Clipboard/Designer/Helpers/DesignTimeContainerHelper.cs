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

            SubstituteFake<IUpdateService>(builder);
            SubstituteFake<IProcessManager>(builder);
            SubstituteFake<IDomainNameResolver>(builder);

            return builder.Build();
        }

        static void SubstituteFake<TInterface>(ContainerBuilder builder) where TInterface : class
        {
            builder.RegisterInstance(Substitute.For<TInterface>()).As<TInterface>();
        }
    }
}
