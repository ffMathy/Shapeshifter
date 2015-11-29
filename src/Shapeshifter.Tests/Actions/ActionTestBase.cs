namespace Shapeshifter.Tests.Actions
{
    using NSubstitute;

    using UserInterface.WindowsDesktop.Data.Interfaces;

    public abstract class ActionTestBase: TestBase
    {
        protected IClipboardDataPackage GetPackageContaining<TData>()
            where TData : class, IClipboardData
        {
            var fakePackage = Substitute.For<IClipboardDataPackage>();
            fakePackage.Contents.Returns(
                                         new IClipboardData[]
                                         {
                                             Substitute.For<TData>()
                                         });

            return fakePackage;
        }

        protected IClipboardDataPackage GetPackageContaining<TData>(params TData[] data)
            where TData : class, IClipboardData
        {
            var fakePackage = Substitute.For<IClipboardDataPackage>();
            fakePackage.Contents.Returns(data);

            return fakePackage;
        }
    }
}