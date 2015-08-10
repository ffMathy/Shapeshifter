using Autofac;
using Ploeh.AutoFixture;
using Shapeshifter.UserInterface.WindowsDesktop;
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

        private Fixture CreateFixture()
        {
            var fixture = new Fixture();
            fixture.OmitAutoProperties = true;
            fixture.RepeatCount = 2;

            //we remove throwing behaviors on circular structures, since this is based on an ORM.
            var behavior = fixture
                .Behaviors
                .OfType<ThrowingRecursionBehavior>()
                .Single();
            fixture.Behaviors.Remove(behavior);

            //we want circular class definitions because this is based on an ORM.
            fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            return fixture;
        }

        protected IContainer CreateContainer(Action<ContainerBuilder> setupCallback = null)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(typeof(App).Assembly).AsImplementedInterfaces();
            if (setupCallback != null)
            {
                setupCallback(builder);
            }

            return builder.Build();
        }
    }
}
