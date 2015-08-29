using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers
{
    static class DesignTimeContainerHelper
    {
        public static IContainer CreateDesignTimeContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DefaultWiringModule());

            return builder.Build();
        }
    }
}
