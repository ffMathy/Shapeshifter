#region

using System;
using System.Windows.Markup;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Converters
{
    public class InjectedConverterMarkupExtension : MarkupExtension
    {
        private static ILifetimeScope container;

        public InjectedConverterMarkupExtension()
        {
            CreateContainerIfNotExists();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return container.Resolve(GetType());
        }

        private static void CreateContainerIfNotExists()
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
                builder.RegisterInstance(environmentInformation).As<IEnvironmentInformation>();

                container = builder.Build();
            }
        }
    }
}