namespace Shapeshifter.WindowsDesktop.Mediators
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;
    using Services.Messages.Interceptors.Hotkeys.Interfaces;

    [TestClass]
    public class PasteCombinationDurationMediatorTest: UnitTestFor<IPasteCombinationDurationMediator>
    {
        static void RaiseHotkeyFired(IPasteHotkeyInterceptor fakePasteHotkeyInterceptor)
        {
            fakePasteHotkeyInterceptor.PasteDetected += Raise.Event();
        }

        [TestMethod]
        public void IsConnectedIsFalseIfConsumerThreadIsNotRunning()
        {
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
            
            Assert.IsFalse(SystemUnderTest.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfConsumerThreadIsRunning()
        {
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);
            
            Assert.IsTrue(SystemUnderTest.IsConnected);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectingWhileNotConnectedThrowsError()
        {
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
            
            SystemUnderTest.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectingWhileAlreadyConnectedThrowsError()
        {
            Container.Resolve<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);

            SystemUnderTest.Connect();
        }

        [TestMethod]
        public void WhenCtrlVIsDownMonitoringLoopRuns()
        {
            SystemUnderTest.Connect();

            var fakePasteHotkeyInterceptor = Container.Resolve<IPasteHotkeyInterceptor>();
            RaiseHotkeyFired(fakePasteHotkeyInterceptor);

            var fakeConsumerThreadLoop = Container.Resolve<IConsumerThreadLoop>();
            fakeConsumerThreadLoop
                .Received()
                .Notify(
                    Arg.Any<Func<Task>>(),
                    Arg.Any<CancellationToken>());
        }
    }
}