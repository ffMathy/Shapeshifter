// ReSharper disable AccessToModifiedClosure
namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ThreadLoopTest: TestBase
    {
        static Func<Task> CreateTestAction(IThreadLoop loop, Func<bool> callback)
        {
            return async () => {
                if (callback())
                {
                    loop.Stop();
                }
                await Task.Delay(1);
            };
        }

        [TestMethod]
        public async Task StartRunsMethod()
        {
            var container = CreateContainer();
            var loop = container.Resolve<IThreadLoop>();

            var invoked = false;
            var action = CreateTestAction(
                loop,
                () => invoked = true);

            await loop.StartAsync(action, CancellationToken.None);

            Assert.IsTrue(invoked);
            Assert.IsFalse(loop.IsRunning);
        }

        [TestMethod]
        public async Task KeepsRunningMethod()
        {
            var container = CreateContainer();
            var loop = container.Resolve<IThreadLoop>();

            var runCount = 0;
            var action = CreateTestAction(
                loop,
                () => ++runCount == 3);

            await loop.StartAsync(action, CancellationToken.None);

            Assert.AreEqual(3, runCount);
            Assert.IsFalse(loop.IsRunning);
        }

        [TestMethod]
        [ExpectedException(typeof(TestException))]
        public async Task ForwardsExceptionsThrown()
        {
            var container = CreateContainer();
            var loop = container.Resolve<IThreadLoop>();
            
            var action = CreateTestAction(
                loop,
                () => {
                    throw new TestException();
                });

            await loop.StartAsync(action, CancellationToken.None);
        }

        [TestMethod]
        public async Task StopsLoopWhenExceptionIsThrown()
        {
            var container = CreateContainer();
            var loop = container.Resolve<IThreadLoop>();

            var action = CreateTestAction(
                loop,
                () => {
                    throw new TestException();
                });

            try
            {
                await loop.StartAsync(action, CancellationToken.None);

                Assert.Fail();
            }
            catch (TestException)
            {
                Assert.IsFalse(loop.IsRunning);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StartTwiceThrowsException()
        {
            var container = CreateContainer();
            var loop = container.Resolve<IThreadLoop>();

            var shouldEndFirstAction = false;
            var firstAction = CreateTestAction(
                loop,
                () => shouldEndFirstAction);
            var secondAction = CreateTestAction(
                loop,
                () => shouldEndFirstAction = true);

            loop.StartAsync(firstAction).IgnoreAwait();

            try
            {
                await loop.StartAsync(secondAction);
            }
            catch
            {
                shouldEndFirstAction = true;
                throw;
            }
        }
    }
}