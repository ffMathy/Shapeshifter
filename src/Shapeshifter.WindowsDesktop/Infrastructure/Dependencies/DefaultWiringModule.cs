using AutofacModule = Autofac.Module;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Dependencies
{
    using System;
    using System.Windows.Threading;

    using Autofac;

    using Controls.Clipboard.Designer.Helpers;

    using Environment;
    using Environment.Interfaces;

    using Logging;

    using Native;

    using Threading;
    using Logging.Interfaces;
    using Services.Files.Interfaces;

    public class DefaultWiringModule: AutofacModule
    {
        readonly IEnvironmentInformation environmentInformation;

        readonly Action<ContainerBuilder> callback;

        public DefaultWiringModule(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public DefaultWiringModule(Action<ContainerBuilder> callback = null)
            : this(new EnvironmentInformation())
        {
            this.callback = callback;
        }

        protected override void Load(ContainerBuilder builder)
        {
            AssemblyRegistrationHelper
                .RegisterAssemblyTypes(builder, typeof (DefaultWiringModule).Assembly, this.environmentInformation.GetIsInDesignTime());

            AssemblyRegistrationHelper
                .RegisterAssemblyTypes(builder, NativeAssemblyHelper.Assembly, this.environmentInformation.GetIsInDesignTime());

            RegisterMainThread(builder);

            var environmentInformation = RegisterEnvironmentInformation(builder);
            RegisterLogging(environmentInformation, builder);

            if (environmentInformation.GetIsInDesignTime())
            {
                DesignTimeContainerHelper.RegisterFakes(builder);
            }

            callback?.Invoke(builder);

            base.Load(builder);
        }

        static void RegisterLogging(IEnvironmentInformation environment, ContainerBuilder builder)
        {
            if (!environment.GetIsDebugging())
            {
                builder
                    .RegisterType<FileLogStream>()
                    .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                    .AsImplementedInterfaces()
                    .SingleInstance();
            }
            else
            {
                builder
                    .RegisterType<DebugLogStream>()
                    .AsImplementedInterfaces()
                    .SingleInstance();
            }
        }

        static void RegisterMainThread(ContainerBuilder builder)
        {
            builder.RegisterInstance(new UserInterfaceThread(Dispatcher.CurrentDispatcher))
                   .AsImplementedInterfaces();
        }

        IEnvironmentInformation RegisterEnvironmentInformation(ContainerBuilder builder)
        {
            var environmentInformation = this.environmentInformation ?? new EnvironmentInformation();
            builder
                .RegisterInstance(environmentInformation)
                .As<IEnvironmentInformation>()
                .SingleInstance();
            return environmentInformation;
        }
    }
}