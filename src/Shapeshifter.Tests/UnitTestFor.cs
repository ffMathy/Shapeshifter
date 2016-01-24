namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Collections.Generic;

    using Autofac;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    public abstract class UnitTestFor<TSystemUnderTest>
        where TSystemUnderTest : class
    {
        protected ILifetimeScope container;
        protected TSystemUnderTest systemUnderTest;

        readonly List<Type> fakeExceptions;
        readonly List<Type> fakeInclusions;

        protected UnitTestFor()
        {
            fakeExceptions = new List<Type>();
            fakeInclusions = new List<Type>();
        }

        protected void ExcludeFakeFor<T>()
        {
            fakeExceptions.Add(typeof(T));
        }

        protected void IncludeFakeFor<T>()
        {
            fakeInclusions.Add(typeof(T));
        }

        [TestInitialize]
        public void Setup()
        {
            container = CreateContainerWithFakeDependencies(
                (c) => {
                    foreach (var fake in fakeInclusions)
                    {
                        var method = typeof (Extensions)
                            .GetMethod(
                                nameof(Extensions.RegisterFake))
                            .MakeGenericMethod(fake);
                        method.Invoke(null, new object[] { c });
                    }
                },
                fakeExceptions.ToArray());
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

        ILifetimeScope CreateContainerWithFakeDependencies(
            Action<ContainerBuilder> setupCallback,
            params Type[] exceptTypes)
        {
            return CreateContainerWithCallback(
                c => {
                    c.RegisterFakesForDependencies<TSystemUnderTest>(exceptTypes);
                    setupCallback(c);
                });
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