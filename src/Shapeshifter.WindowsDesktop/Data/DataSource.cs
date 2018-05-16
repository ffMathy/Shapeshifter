namespace Shapeshifter.WindowsDesktop.Data
{
    using Interfaces;

    public class DataSource : IDataSource
    {
        public DataSource(byte[] iconBig, byte[] iconSmall, string text)
        {
            IconLarge = iconBig;
            IconSmall = iconSmall;
            Title = text;
        }

        public byte[] IconLarge { get; }
        public byte[] IconSmall { get; }

        public string Title { get; }
    }
}