using AutofacModule = Autofac.Module;

namespace Shapeshifter.UserInterface.WindowsDesktop.Installer.Infrastructure.Dependencies
{
    using Autofac;

    using Shapeshifter.WindowsDesktop.Shared;
    using Shapeshifter.WindowsDesktop.Shared.Infrastructure.Dependencies;

    public class DefaultWiringModule : AutofacModule
    {

        protected override void Load(ContainerBuilder builder)
        {
            AssemblyRegistrationHelper
                .RegisterAssemblyTypes(builder, typeof(DefaultWiringModule).Assembly, false);
            AssemblyRegistrationHelper
                .RegisterAssemblyTypes(builder, SharedAssemblyHelper.Assembly, false);

            base.Load(builder);
        }
    }
}