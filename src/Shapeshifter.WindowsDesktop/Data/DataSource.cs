namespace Shapeshifter.WindowsDesktop.Data
{
    using Interfaces;

    public class DataSource : IDataSource
    {
        public DataSource(byte[] iconBig, byte[] iconSmall, string text)
        {
            IconLarge = iconBig;
            IconSmall = iconSmall;
            Text = text;
        }

        public byte[] IconLarge { get; }
        public byte[] IconSmall { get; }

        public string Text { get; }
    }
}