namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Converters
{
    using System;
    using System.Windows.Markup;

    using Autofac;

    using Clipboard.Designer.Helpers;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment;
    using Infrastructure.Environment.Interfaces;

    public class InjectedConverterMarkupExtension: MarkupExtension
    {
        static ILifetimeScope container;

        public InjectedConverterMarkupExtension()
        {
            CreateContainerIfNotExists();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return container.Resolve(GetType());
        }

        static void CreateContainerIfNotExists()
        {
            if (container != null)
            {
                return;
            }

            var environmentInformation = new EnvironmentInformation();
            if (environmentInformation.IsInDesignTime)
            {
                container = DesignTimeContainerHelper.CreateDesignTimeContainer();
            }
            else
            {
                var builder = new ContainerBuilder();
                builder.RegisterModule(new DefaultWiringModule());
                builder.RegisterInstance(environmentInformation)
                       .As<IEnvironmentInformation>();

                container = builder.Build();
            }
        }
    }
}