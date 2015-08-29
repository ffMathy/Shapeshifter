using NSubstitute;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.Tests.Actions
{
    public abstract class ActionTestBase : TestBase
    {
        protected IClipboardDataPackage GetPackageContaining<TData>() where TData : class, IClipboardData
        {
            var fakePackage = Substitute.For<IClipboardDataPackage>();
            fakePackage.Contents.Returns(
                new IClipboardData[] { Substitute.For<TData>() });

            return fakePackage;
        }

        protected IClipboardDataPackage GetPackageContaining<TData>(TData data) where TData : class, IClipboardData
        {
            var fakePackage = Substitute.For<IClipboardDataPackage>();
            fakePackage.Contents.Returns(
                new IClipboardData[] { data });

            return fakePackage;
        }
    }
}
