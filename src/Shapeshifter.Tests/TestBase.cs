using Autofac;
using NSubstitute;
using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies;
using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System;

namespace Shapeshifter.WindowsDesktop
{
	public abstract class TestBase
    {

        protected static ILifetimeScope CreateContainer(
            Action<ContainerBuilder> setupCallback = null)
        {
            var builder = new ContainerBuilder();

            var fakeEnvironment = builder.RegisterFake<IEnvironmentInformation>();

            fakeEnvironment
                .GetIsInDesignTime()
                .Returns(false);

            fakeEnvironment
                .GetIsDebugging()
                .Returns(true);

            builder.RegisterModule(
                new DefaultWiringModule(fakeEnvironment));

            setupCallback?.Invoke(builder);

            var result = builder
                .Build()
                .BeginLifetimeScope();

            return result;
        }
    }
}
