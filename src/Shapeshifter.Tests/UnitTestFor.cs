namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Autofac;

    using Controls.Window.ViewModels.Interfaces;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Files.Interfaces;
    using NCrunch.Framework;

    [Serial]
    public abstract class UnitTestFor<TSystemUnderTest>: TestBase
        where TSystemUnderTest : class
    {

        ILifetimeScope container;

        /// <summary>
        /// Represents the container, which has the class of the unit's dependencies automatically registered as fakes. You can use the <see cref="IncludeFakeFor{T}"/> and <see cref="ExcludeFakeFor{T}"/> method to exclude or include fakes other than the ones automatically registered.
        /// </summary>
        protected ILifetimeScope Container
        {
            get
            {
                Setup();
                return container;
            }
        }

        TSystemUnderTest systemUnderTest;

        /// <summary>
        /// Represents the class of the unit being tested, with all its dependencies registered as fakes in the container automatically. You can use the <see cref="IncludeFakeFor{T}"/> and <see cref="ExcludeFakeFor{T}"/> method to exclude or include fakes other than the ones automatically registered.
        /// </summary>
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
                        Extensions.RegisterFake(c, fake);
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

            var temporaryPath = Path.Combine(
                Path.GetTempPath(),
                "Shapeshifter");
            if (Directory.Exists(temporaryPath)) {
                Directory.Delete(temporaryPath, true);
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
            return CreateContainer(
                c =>
                {
                    c.RegisterFakesForDependencies<TSystemUnderTest>(exceptTypes);
                    setupCallback(c);
                });
        }
    }
}