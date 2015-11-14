using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class DataSource : IDataSource
    {
        public DataSource(byte[] icon, string text)
        {
            Icon = icon;
            Text = text;
        }

        public byte[] Icon
        {
            get;private set;
        }

        public string Text
        {
            get; private set;
        }
    }
}
