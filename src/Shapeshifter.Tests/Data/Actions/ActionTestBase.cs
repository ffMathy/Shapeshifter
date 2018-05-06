namespace Shapeshifter.WindowsDesktop.Data.Actions
{
	using Infrastructure.Threading.Interfaces;

	public abstract class ActionTestBase<TSystemUnderTest> : UnitTestFor<TSystemUnderTest>
        where TSystemUnderTest: class
    {
        public ActionTestBase()
        {
            ExcludeFakeFor<IAsyncFilter>();
        }
    }
}