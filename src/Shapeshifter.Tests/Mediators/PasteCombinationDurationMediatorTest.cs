using System;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Mediators.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces;

namespace Shapeshifter.Tests.Mediators
{
    [TestClass]
    public class PasteCombinationDurationMediatorTest : TestBase
    {
        [TestMethod]
        public void IsConnectedIsFalseIfConsumerThreadIsNotRunning()
        {
            var container = CreateContainer(c =>
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
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IConsumerThreadLoop>()
                    .IsRunning
                    .Returns(true);
            });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();
            Assert.IsTrue(mediator.IsConnected);
        }

        [TestMethod]
        public void DisconnectingWhileNotConnectedThrowsError()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IConsumerThreadLoop>()
                    .IsRunning
                    .Returns(false);
            });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();

            try
            {
                mediator.Disconnect();
                Assert.Fail("Did not throw an exception.");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [TestMethod]
        public void ConnectingWhileAlreadyConnectedThrowsError()
        {
            var container = CreateContainer(c =>
            {
                c.RegisterFake<IConsumerThreadLoop>()
                    .IsRunning
                    .Returns(true);
            });

            var mediator = container.Resolve<IPasteCombinationDurationMediator>();

            try
            {
                mediator.Connect(Substitute.For<IWindow>());
                Assert.Fail("Did not throw an exception.");
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}