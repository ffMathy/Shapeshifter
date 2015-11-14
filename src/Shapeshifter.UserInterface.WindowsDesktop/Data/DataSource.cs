#region

using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class DataSource : IDataSource
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