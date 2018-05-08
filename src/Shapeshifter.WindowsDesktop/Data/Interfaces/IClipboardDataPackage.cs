namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
	using System;
	using System.Collections.Generic;

	public interface IClipboardDataPackage
	{
		void AddData(IClipboardData data);

		Guid Id { get; }

		IReadOnlyList<IClipboardData> Contents { get; }
		IDataSource Source { get; }
	}
}