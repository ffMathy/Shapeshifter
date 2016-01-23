// ReSharper disable AccessToModifiedClosure

namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Interfaces;

    using Logging.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ThreadLoopTest: UnitTestFor<IThreadLoop>
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
            var invoked = false;
            var action = CreateTestAction(
                systemUnderTest,
                () => invoked = true);

            await systemUnderTest.StartAsync(
                action, CancellationToken.None);

            Assert.IsTrue(invoked);
            Assert.IsFalse(systemUnderTest.IsRunning);
        }

        [TestMethod]
        public async Task KeepsRunningMethod()
        {
            var runCount = 0;
            var action = CreateTestAction(
                systemUnderTest,
                () => ++runCount == 3);

            await systemUnderTest.StartAsync(
                action, CancellationToken.None);

            Assert.AreEqual(3, runCount);
            Assert.IsFalse(systemUnderTest.IsRunning);
        }

        [TestMethod]
        [ExpectedException(typeof (TestException))]
        public async Task ForwardsExceptionsThrown()
        {
            var action = CreateTestAction(
                systemUnderTest,
                () => {
                    throw new TestException();
                });

            await systemUnderTest.StartAsync(
                action, CancellationToken.None);
        }

        [TestMethod]
        public async Task StopsLoopWhenExceptionIsThrown()
        {
            var action = CreateTestAction(
                systemUnderTest,
                () => {
                    throw new TestException();
                });

            try
            {
                await systemUnderTest.StartAsync(
                    action, CancellationToken.None);

                Assert.Fail();
            }
            catch (TestException)
            {
                Assert.IsFalse(systemUnderTest.IsRunning);
            }
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public async Task StartTwiceThrowsException()
        {
            var shouldEndFirstAction = false;
            var firstAction = CreateTestAction(
                systemUnderTest,
                () => shouldEndFirstAction);
            var secondAction = CreateTestAction(
                systemUnderTest,
                () => shouldEndFirstAction = true);

            systemUnderTest
                .StartAsync(firstAction)
                .IgnoreAwait();

            try
            {
                await systemUnderTest.StartAsync(secondAction);
            }
            catch
            {
                shouldEndFirstAction = true;
                throw;
            }
        }
    }
}