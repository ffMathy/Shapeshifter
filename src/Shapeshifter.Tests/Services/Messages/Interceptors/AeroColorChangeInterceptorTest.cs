using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shapeshifter.WindowsDesktop.Services.Messages.Interceptors.Interfaces;
using System;

namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
    [TestClass]
    public class AeroColorChangeInterceptorTest : UnitTestFor<IAeroColorChangeInterceptor>
    {
        [TestMethod]
        public void InstallingIntereceptorUpdatesAccentDarkColor()
        {
            SystemUnderTest.Install(IntPtr.Zero);
        }
    }
}
