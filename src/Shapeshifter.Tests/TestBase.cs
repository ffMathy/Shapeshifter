namespace Shapeshifter.UserInterface.WindowsDesktop
{
    using System;
    using System.Linq;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Ploeh.AutoFixture;

    using Infrastructure.Dependencies;
    using Infrastructure.Environment.Interfaces;
    using Services.Interfaces;

    public abstract class TestBase
    {
        protected Fixture fixture;

        [TestCleanup]
        public void ClearCacheOnEnd()
        {
            Extensions.ClearCache();
        }

        protected TestBase()
        {
            fixture = CreateFixture();
        }

        static Fixture CreateFixture()
        {
            var fixture = new Fixture
            {
                OmitAutoProperties = true,
                RepeatCount = 5
            };

            var behavior = fixture
                .Behaviors
                .OfType<ThrowingRecursionBehavior>()
                .Single();
            fixture.Behaviors.Remove(behavior);

            fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            return fixture;
        }

        protected ILifetimeScope CreateContainer(Action<ContainerBuilder> setupCallback = null)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DefaultWiringModule());

            builder.RegisterFake<IEnvironmentInformation>()
                   .IsInDesignTime
                   .Returns(false);
            builder.RegisterFake<IUpdateService>();

            setupCallback?.Invoke(builder);

            return builder.Build();
        }
    }
}