namespace Shapeshifter.WindowsDesktop.Data.Interfaces
{
	using System;
	using System.Collections.Generic;

	using Actions.Interfaces;

	public interface IClipboardDataPackage
	{
		void AddData(IClipboardData data);

		Guid Id { get; }

		IReadOnlyList<IClipboardData> Contents { get; }
		IReadOnlyList<IAction> Actions { get; }

		IDataSource Source { get; }

		string ContentHash { get; }

		void PopulateCompatibleActionsAsync(IEnumerable<IAction> actionCandidates);
	}
}