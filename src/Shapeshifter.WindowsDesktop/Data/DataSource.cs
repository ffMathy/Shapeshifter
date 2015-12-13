namespace Shapeshifter.WindowsDesktop.Data
{
    using Interfaces;

    public class DataSource: IDataSource
    {
        public DataSource(byte[] icon, string text)
        {
            Icon = icon;
            Text = text;
        }

        public byte[] Icon { get; }

        public string Text { get; }
    }
}