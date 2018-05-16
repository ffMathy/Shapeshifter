namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
    public interface IDataSource
    {
        byte[] IconLarge { get; }

        byte[] IconSmall { get; }

        string Title { get; }

		string ProcessName { get; }
    }
}