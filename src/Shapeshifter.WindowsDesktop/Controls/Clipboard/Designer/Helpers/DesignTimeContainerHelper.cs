namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Helpers
{
    using WindowsDesktop.Services.Web.Interfaces;

    using Autofac;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment;

    using Services;

    using Shared.Services.Files.Interfaces;
    using Shared.Services.Interfaces;

    static class DesignTimeContainerHelper
    {
        public static IContainer CreateDesignTimeContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DefaultWiringModule(
                new EnvironmentInformation(true)));

            return builder.Build();
        }

        public static void RegisterFakes(ContainerBuilder builder)
        {
            builder.RegisterType<DesignerFileManager>()
                   .AsSelf()
                   .As<IFileManager>()
                   .SingleInstance();
            builder.RegisterType<DesignerProcessManager>()
                   .AsSelf()
                   .As<IProcessManager>()
                   .SingleInstance();
            builder.RegisterType<DesignerDomainNameResolver>()
                   .AsSelf()
                   .As<IDomainNameResolver>()
                   .SingleInstance();
        }
    }
}