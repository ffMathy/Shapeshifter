namespace Shapeshifter.WindowsDesktop
{
    using System;

    using Autofac;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    public abstract class TestBase
    {
        ILifetimeScope activeContainer;

        [TestCleanup]
        public void ClearCacheOnEnd()
        {
            Extensions.ClearCache();
            DisposeContainer();
        }

        void DisposeContainer()
        {
            activeContainer?.Dispose();
            activeContainer = null;
        }

        protected ILifetimeScope CreateContainer(Action<ContainerBuilder> setupCallback = null)
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

            builder.RegisterFake<IThreadDelay>();

            setupCallback?.Invoke(builder);

            return activeContainer = builder
                                         .Build()
                                         .BeginLifetimeScope();
        }
    }
}