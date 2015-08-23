using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;
using System;
using System.Windows.Markup;

namespace Shapeshifter.UserInterface.WindowsDesktop.Converters
{
    public abstract class InjectedConverterMarkupExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var container = GetContainer();
            return container.Resolve(GetType());
        }

        static ILifetimeScope GetContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DefaultWiringModule());
            return builder.Build();
        }
    }
}
