using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System;
using System.Windows.Markup;

namespace Shapeshifter.UserInterface.WindowsDesktop.Converters
{
    public class InjectedConverterMarkupExtension : MarkupExtension
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
            if(container != null)
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
                builder.RegisterInstance(environmentInformation).As<IEnvironmentInformation>();

                container = builder.Build();
            }
        }
    }
}
