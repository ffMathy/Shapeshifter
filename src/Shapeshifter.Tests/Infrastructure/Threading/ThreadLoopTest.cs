// ReSharper disable AccessToModifiedClosure

namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Interfaces;

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
                SystemUnderTest,
                () => invoked = true);

            await SystemUnderTest.StartAsync(
                action, CancellationToken.None);

            Assert.IsTrue(invoked);
            Assert.IsFalse(SystemUnderTest.IsRunning);
        }

        [TestMethod]
        public async Task KeepsRunningMethod()
        {
            var runCount = 0;
            var action = CreateTestAction(
                SystemUnderTest,
                () => ++runCount == 3);

            await SystemUnderTest.StartAsync(
                action, CancellationToken.None);

            Assert.AreEqual(3, runCount);
            Assert.IsFalse(SystemUnderTest.IsRunning);
        }

        [TestMethod]
        [ExpectedException(typeof (TestException))]
        public async Task ForwardsExceptionsThrown()
        {
            var action = CreateTestAction(
                SystemUnderTest,
                () => {
                    throw new TestException();
                });

            await SystemUnderTest.StartAsync(
                action, CancellationToken.None);
        }

        [TestMethod]
        public async Task StopsLoopWhenExceptionIsThrown()
        {
            var action = CreateTestAction(
                SystemUnderTest,
                () => {
                    throw new TestException();
                });

            try
            {
                await SystemUnderTest.StartAsync(
                    action, CancellationToken.None);

                Assert.Fail();
            }
            catch (TestException)
            {
                Assert.IsFalse(SystemUnderTest.IsRunning);
            }
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public async Task StartTwiceThrowsException()
        {
            var shouldEndFirstAction = false;
            var firstAction = CreateTestAction(
                SystemUnderTest,
                () => shouldEndFirstAction);
            var secondAction = CreateTestAction(
                SystemUnderTest,
                () => shouldEndFirstAction = true);

            SystemUnderTest
                .StartAsync(firstAction)
                .IgnoreAwait();

            try
            {
                await SystemUnderTest.StartAsync(secondAction);
            }
            catch
            {
                shouldEndFirstAction = true;
                throw;
            }
        }
    }
}