namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
    public interface IDataSource
    {
        byte[] IconLarge { get; }

        byte[] IconSmall { get; }

        string Text { get; }
    }
}