namespace Shapeshifter.WindowsDesktop.Data
{
    using Interfaces;

    public class DataSource : IDataSource
    {
        public DataSource(byte[] iconBig, byte[] iconSmall, string text, string processName)
        {
            IconLarge = iconBig;
            IconSmall = iconSmall;
            Title = text;
			ProcessName = processName;
		}

        public byte[] IconLarge { get; }
        public byte[] IconSmall { get; }

        public string Title { get; }
		public string ProcessName { get; }
	}
}