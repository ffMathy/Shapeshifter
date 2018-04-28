namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using NSubstitute;

    public abstract class ActionTestBase<TSystemUnderTest> : UnitTestFor<TSystemUnderTest>
        where TSystemUnderTest: class
    {
        public ActionTestBase()
        {
            ExcludeFakeFor<IAsyncFilter>();
        }
    }
}