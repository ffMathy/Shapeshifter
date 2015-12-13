namespace Shapeshifter.WindowsDesktop
{
    using System;

    using Autofac;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;
    using Infrastructure.Threading.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Interfaces;

    public abstract class TestBase
    {
        [TestCleanup]
        public void ClearCacheOnEnd()
        {
            Extensions.ClearCache();
        }

        protected ILifetimeScope CreateContainer(Action<ContainerBuilder> setupCallback = null)
        {
            var builder = new ContainerBuilder();

            var fakeEnvironment = builder.RegisterFake<IEnvironmentInformation>();
            fakeEnvironment
                .IsInDesignTime
                .Returns(false);

            builder.RegisterModule(
                new DefaultWiringModule(fakeEnvironment));

            builder.RegisterFake<IUpdateService>();
            builder.RegisterFake<IThreadDelay>();

            setupCallback?.Invoke(builder);

            return builder.Build();
        }
    }
}