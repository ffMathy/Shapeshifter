using System.Diagnostics.CodeAnalysis;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Web.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers
{
    [ExcludeFromCodeCoverage]
    internal static class DesignTimeContainerHelper
    {
        public static IContainer CreateDesignTimeContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DefaultWiringModule());

            return builder.Build();
        }

        public static void RegisterFakes(ContainerBuilder builder)
        {
            builder.RegisterType<DesignerUpdateService>().AsSelf().As<IUpdateService>().SingleInstance();
            builder.RegisterType<DesignerFileManager>().AsSelf().As<IFileManager>().SingleInstance();
            builder.RegisterType<DesignerProcessManager>().AsSelf().As<IProcessManager>().SingleInstance();
            builder.RegisterType<DesignerDomainNameResolver>().AsSelf().As<IDomainNameResolver>().SingleInstance();
        }
    }
}