namespace Shapeshifter.WindowsDesktop.Data
{
	using System;
	using System.Collections.Generic;

	using Interfaces;

	public class ClipboardDataPackage : IClipboardDataPackage
	{
		readonly List<IClipboardData> dataCollection;

		public ClipboardDataPackage()
		{
			dataCollection = new List<IClipboardData>();
			Id = Guid.NewGuid();
		}

		public ClipboardDataPackage(Guid id)
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

		public Guid Id { get; set; }

		public IDataSource Source { get; set; }
	}
}