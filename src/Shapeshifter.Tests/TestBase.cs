using Autofac;
using Ploeh.AutoFixture;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;
using System;
using System.Linq;

namespace Shapeshifter.Tests
{
    public abstract class TestBase
    {
        protected Fixture fixture;

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

            if(setupCallback != null) {
                setupCallback(builder);
            }

            return builder.Build();
        }
    }
}
