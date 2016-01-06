namespace Shapeshifter.WindowsDesktop
{
    using System;

    using Autofac;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Files.Interfaces;
    using Services.Interfaces;

    public abstract class TestBase
    {
        ILifetimeScope activeContainer;

        [TestCleanup]
        public void ClearCacheOnEnd()
        {
            Extensions.ClearCache();
            DisposeIntegrationTestTypes();
        }

        void DisposeIntegrationTestTypes()
        {
            if (activeContainer == null)
            {
                return;
            }

            var fileManager = activeContainer.Resolve<IFileManager>();
            fileManager.DeleteDirectoryIfExists(fileManager.PrepareFolder());
            fileManager.Dispose();
        }

        protected ILifetimeScope CreateContainer(Action<ContainerBuilder> setupCallback = null)
        {
            var builder = new ContainerBuilder();

            var fakeEnvironment = builder.RegisterFake<IEnvironmentInformation>();

            fakeEnvironment
                .IsInDesignTime
                .Returns(false);

            fakeEnvironment
                .IsDebugging
                .Returns(true);

            builder.RegisterModule(
                new DefaultWiringModule(fakeEnvironment));

            builder.RegisterFake<IUpdateService>();
            builder.RegisterFake<IThreadDelay>();

            setupCallback?.Invoke(builder);

            return activeContainer = builder.Build();
        }
    }
}