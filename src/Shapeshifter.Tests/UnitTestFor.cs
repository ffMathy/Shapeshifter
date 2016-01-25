namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Autofac;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Files.Interfaces;

    public abstract class UnitTestFor<TSystemUnderTest>
        where TSystemUnderTest : class
    {

        ILifetimeScope container;
        protected ILifetimeScope Container
        {
            get
            {
                Setup();
                return container;
            }
        }

        TSystemUnderTest systemUnderTest;
        protected TSystemUnderTest SystemUnderTest
        {
            get
            {
                Setup();
                return systemUnderTest;
            }
        }

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

        void Setup()
        {
            if ((container != null) && (systemUnderTest != null))
            {
                return;
            }

            container = CreateContainerWithFakeDependencies(
                (c) => {
                    foreach (var fake in fakeInclusions)
                    {
                        var method = typeof (Extensions)
                            .GetMethod(
                                nameof(Extensions.RegisterFake))
                            .MakeGenericMethod(fake);
                        method.Invoke(
                            null,
                            new object[]
                            {
                                c
                            });
                    }
                },
                fakeExceptions.ToArray());
            systemUnderTest = container.Resolve<TSystemUnderTest>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            var fileManager = Container.Resolve<IFileManager>();
            var folder = fileManager.PrepareIsolatedFolder();
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.Delete(folder, true);
            }

            fakeExceptions.Clear();
            fakeInclusions.Clear();

            Extensions.ClearCache();
            DisposeContainer();
        }

        void DisposeContainer()
        {
            container?.Dispose();
            container = null;
        }

        static ILifetimeScope CreateContainerWithFakeDependencies(
            Action<ContainerBuilder> setupCallback,
            params Type[] exceptTypes)
        {
            return CreateContainerWithCallback(
                c =>
                {
                    c.RegisterFakesForDependencies<TSystemUnderTest>(exceptTypes);
                    setupCallback(c);
                });
        }

        static ILifetimeScope CreateContainerWithCallback(
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

            return builder
                .Build()
                .BeginLifetimeScope();
        }
    }
}