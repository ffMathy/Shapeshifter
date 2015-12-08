namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Interfaces;

    [TestClass]
    public class ConsumerThreadLoopTest: TestBase
    {
        [TestMethod]
        public void IsRunningWhenInnerLoopIsRunning()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IThreadLoop>()
                     .IsRunning
                     .Returns(true);
                });

            var systemUnderTest = container.Resolve<IConsumerThreadLoop>();
            Assert.IsTrue(systemUnderTest.IsRunning);
        }

        [TestMethod]
        public void IsNotRunningWhenInnerLoopIsNotRunning()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IThreadLoop>()
                     .IsRunning
                     .Returns(false);
                });

            var systemUnderTest = container.Resolve<IConsumerThreadLoop>();
            Assert.IsFalse(systemUnderTest.IsRunning);
        }

        [TestMethod]
        public void StopStopsInnerLoop()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IThreadLoop>();
                });

            var systemUnderTest = container.Resolve<IConsumerThreadLoop>();
            systemUnderTest.Stop();

            var fakeInnerLoop = container.Resolve<IThreadLoop>();
            fakeInnerLoop.Received()
                         .Stop();
        }

        [TestMethod]
        public void NotifyForTheFirstTimeSpawnsThread()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IThreadLoop>();
                });

            var systemUnderTest = container.Resolve<IConsumerThreadLoop>();
            systemUnderTest.Notify(() => Task.CompletedTask, CancellationToken.None);

            var fakeInnerLoop = container.Resolve<IThreadLoop>();
            fakeInnerLoop.Received()
                         .StartAsync(Arg.Any<Func<Task>>(), CancellationToken.None);
        }

        [TestMethod]
        public async Task LoopStopsWhenNothingIsLeftToProcess()
        {
            IThreadLoop fakeInnerLoop = null;
            Func<Task> innerLoopTick = null;
            var container = CreateContainer(
                c =>
                {
                    fakeInnerLoop = c.RegisterFake<IThreadLoop>();
                    fakeInnerLoop.StartAsync(
                        Arg.Do<Func<Task>>(
                            x =>
                            {
                                innerLoopTick
                                    = x;
                            }),
                        CancellationToken.None);
                });

            var systemUnderTest = container.Resolve<IConsumerThreadLoop>();

            systemUnderTest.Notify(() => Task.CompletedTask, CancellationToken.None);
            systemUnderTest.Notify(() => Task.CompletedTask, CancellationToken.None);
            systemUnderTest.Notify(() => Task.CompletedTask, CancellationToken.None);

            await innerLoopTick();
            await innerLoopTick();
            await innerLoopTick();

            fakeInnerLoop.DidNotReceive()
                         .Stop();

            await innerLoopTick();

            fakeInnerLoop.Received(1)
                         .Stop();
        }
    }
}