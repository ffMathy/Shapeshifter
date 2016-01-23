namespace Shapeshifter.WindowsDesktop
{
    using System;

    using Autofac;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    public abstract class UnitTestFor<TSystemUnderTest>
        where TSystemUnderTest : class
    {
        protected ILifetimeScope container;
        protected TSystemUnderTest systemUnderTest;

        [TestInitialize]
        public void Setup()
        {
            container = CreateContainerWithFakeDependencies();
            systemUnderTest = container.Resolve<TSystemUnderTest>();
        }

        [TestCleanup]
        public void ClearCacheOnEnd()
        {
            Extensions.ClearCache();
            DisposeContainer();
        }

        void DisposeContainer()
        {
            container?.Dispose();
            container = null;
        }

        ILifetimeScope CreateContainerWithFakeDependencies()
        {
            return CreateContainerWithCallback(c => c.RegisterFakesForDependencies<TSystemUnderTest>());
        }

        ILifetimeScope CreateContainerWithCallback(
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

            return container = builder
                .Build()
                .BeginLifetimeScope();
        }
    }
}