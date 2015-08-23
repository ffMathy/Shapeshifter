using Autofac;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers
{
    static class DesignTimeContainerHelper
    {
        public static IContainer CreateDesignTimeContainer()
        {
            var builder = new ContainerBuilder();


            return builder.Build();
        }
    }
}
