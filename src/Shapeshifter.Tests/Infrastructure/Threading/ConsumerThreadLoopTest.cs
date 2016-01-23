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
            container
                .Resolve<IThreadLoop>()
                .IsRunning
                .Returns(true);

            Assert.IsTrue(systemUnderTest.IsRunning);
        }

        [TestMethod]
        public void IsNotRunningWhenInnerLoopIsNotRunning()
        {
            container
                .Resolve<IThreadLoop>()
                .IsRunning
                .Returns(false);

            Assert.IsFalse(systemUnderTest.IsRunning);
        }

        [TestMethod]
        public void StopStopsInnerLoop()
        {
            systemUnderTest.Stop();

            container.Resolve<IThreadLoop>()
                     .Received()
                     .Stop();
        }

        [TestMethod]
        public void NotifyForTheFirstTimeSpawnsThread()
        {
            systemUnderTest.Notify(() => Task.CompletedTask, CancellationToken.None);

            container
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

            var fakeInnerLoop = container.Resolve<IThreadLoop>();
            fakeInnerLoop.StartAsync(
                    Arg.Do<Func<Task>>(
                        x =>
                        {
                            innerLoopTick
                                = x;
                        }),
                    CancellationToken.None)
                .IgnoreAwait();

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