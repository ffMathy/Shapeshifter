using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using System;
using System.Linq;

namespace Shapeshifter.Tests
{
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

        Fixture CreateFixture()
        {
            var fixture = new Fixture();
            fixture.OmitAutoProperties = true;
            fixture.RepeatCount = 5;
            
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

            builder.RegisterFake<IUpdateService>();

            if(setupCallback != null) {
                setupCallback(builder);
            }

            return builder.Build();
        }
    }
}
