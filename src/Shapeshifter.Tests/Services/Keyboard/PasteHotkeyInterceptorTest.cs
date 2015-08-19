using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Keyboard.Interfaces;
using System.Windows.Input;

namespace Shapeshifter.Tests.Services.Keyboard
{
    [TestClass]
    public class PasteHotkeyInterceptorTest : TestBase
    {
        private void AssertDoesNotBlockWhenKeyDownReceived(IPasteHotkeyInterceptor interceptor, Key key)
        {
            Assert.IsFalse(interceptor.ShouldBlockKeyDown(key));
            interceptor.ReceiveKeyDown(key);
        }

        private void AssertDoesNotBlockWhenKeyUpReceived(IPasteHotkeyInterceptor interceptor, Key key)
        {
            Assert.IsFalse(interceptor.ShouldBlockKeyUp(key));
            interceptor.ReceiveKeyUp(key);
        }

        [TestMethod]
        public void IfLeftShiftIsDownThenCtrlVHotkeyIsNeverBlockedInOrderCtrlDownVDownVUpCtrlUp()
        {
            var container = CreateContainer();

            var interceptor = container.Resolve<IPasteHotkeyInterceptor>();

            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.LeftShift);

            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.LeftCtrl);
            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.V);

            AssertDoesNotBlockWhenKeyUpReceived(interceptor, Key.V);
            AssertDoesNotBlockWhenKeyUpReceived(interceptor, Key.LeftCtrl);
        }

        [TestMethod]
        public void IfLeftShiftIsDownThenCtrlVHotkeyIsNeverBlockedInOrderCtrlDownVDownCtrlUpVUp()
        {
            var container = CreateContainer();

            var interceptor = container.Resolve<IPasteHotkeyInterceptor>();

            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.LeftShift);

            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.LeftCtrl);
            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.V);

            AssertDoesNotBlockWhenKeyUpReceived(interceptor, Key.LeftCtrl);
            AssertDoesNotBlockWhenKeyUpReceived(interceptor, Key.V);
        }

        [TestMethod]
        public void IfCtrlIsDownThenCtrlVHotkeyStayingDownIsBlocked()
        {
            var container = CreateContainer();

            var interceptor = container.Resolve<IPasteHotkeyInterceptor>();
            
            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.LeftCtrl);
            AssertDoesNotBlockWhenKeyDownReceived(interceptor, Key.V);

            Assert.IsTrue(interceptor.ShouldBlockKeyDown(Key.V));
            Assert.IsTrue(interceptor.ShouldBlockKeyDown(Key.LeftCtrl));
        }
    }
}
