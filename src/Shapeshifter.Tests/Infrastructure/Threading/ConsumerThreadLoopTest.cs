namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    [TestClass]
    public class ConsumerThreadLoopTest : UnitTestFor<IConsumerThreadLoop>
    {
        [TestMethod]
        public void IsRunningWhenInnerLoopIsRunning()
        {
            Container
                .Resolve<IThreadLoop>()
                .IsRunning
                .Returns(true);

            Assert.IsTrue(SystemUnderTest.IsRunning);
        }

        [TestMethod]
        public void IsNotRunningWhenInnerLoopIsNotRunning()
        {
            Container
                .Resolve<IThreadLoop>()
                .IsRunning
                .Returns(false);

            Assert.IsFalse(SystemUnderTest.IsRunning);
        }

        [TestMethod]
        public void StopStopsInnerLoop()
        {
            SystemUnderTest.Stop();

            Container.Resolve<IThreadLoop>()
                     .Received()
                     .Stop();
        }

        [TestMethod]
        public void NotifyForTheFirstTimeSpawnsThread()
        {
            SystemUnderTest.Notify(() => Task.CompletedTask, CancellationToken.None);

            Container
                .Resolve<IThreadLoop>()
                .Received()
                .StartAsync(
                    Arg.Any<Func<Task>>(),
                    CancellationToken.None);
        }

        [TestMethod]
        public async Task LoopStopsWhenNothingIsLeftToProcess()
        {
            Func<Task> innerLoopTick = null;

            var fakeInnerLoop = Container.Resolve<IThreadLoop>();
            fakeInnerLoop.StartAsync(
                    Arg.Do<Func<Task>>(
                        x =>
                        {
                            innerLoopTick
                                = x;
                        }),
                    CancellationToken.None)
                .IgnoreAwait();

            var systemUnderTest = Container.Resolve<IConsumerThreadLoop>();

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