namespace Shapeshifter.Tests.Mediators
{
    using System;

    using Autofac;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
    using UserInterface.WindowsDesktop.Mediators.Interfaces;
    using UserInterface.WindowsDesktop.Windows.Interfaces;

    [TestClass]
    public class PasteCombinationDurationMediatorTest: TestBase
    {
        [TestMethod]
        public void IsConnectedIsFalseIfConsumerThreadIsNotRunning()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsFalse(mediator.IsConnected);
        }

        [TestMethod]
        public void IsConnectedIsTrueIfConsumerThreadIsRunning()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsTrue(mediator.IsConnected);
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void DisconnectingWhileNotConnectedThrowsError()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(false);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.Disconnect();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void ConnectingWhileAlreadyConnectedThrowsError()
        {
            var container = CreateContainer(
                c =>
                {
                    c.RegisterFake<IConsumerThreadLoop>()
                     .IsRunning
                     .Returns(true);
                });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            mediator.Connect(Substitute.For<IWindow>());
        }
    }
}