namespace Shapeshifter.WindowsDesktop.Data
{
	using System.Collections.Generic;
	using System.Threading;

	using Interfaces;

	public class ClipboardDataPackage : IClipboardDataPackage
	{
		readonly List<IClipboardData> dataCollection;

		static long idOffset;

		public ClipboardDataPackage()
		{
			dataCollection = new List<IClipboardData>();
			Id = Interlocked.Increment(ref idOffset);
		}

		public ClipboardDataPackage(long id)
			: this()
		{
			Id = id;
		}

		public IReadOnlyList<IClipboardData> Contents =>
			dataCollection;

		public void AddData(IClipboardData data)
		{
			data.Package = this;
			dataCollection.Add(data);
		}

		public long Id { get; }

		public IDataSource Source { get; set; }
	}
}