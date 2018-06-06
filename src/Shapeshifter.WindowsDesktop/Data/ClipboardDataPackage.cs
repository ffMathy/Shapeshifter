namespace Shapeshifter.WindowsDesktop.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Actions.Interfaces;

	using Interfaces;

	public class ClipboardDataPackage : IClipboardDataPackage
	{
		readonly List<IClipboardData> dataCollection;
		readonly List<IAction> actions;

		public ClipboardDataPackage()
		{
			dataCollection = new List<IClipboardData>();
			actions = new List<IAction>();

			Id = Guid.NewGuid();
		}

		public ClipboardDataPackage(
			Guid id)
			: this()
		{
			Id = id;
		}

		public IReadOnlyList<IClipboardData> Contents =>
			dataCollection;

		public IReadOnlyList<IAction> Actions => actions;

		public void AddData(IClipboardData data)
		{
			data.Package = this;
			dataCollection.Add(data);
		}

		public async void PopulateCompatibleActionsAsync(IEnumerable<IAction> actionCandidates)
		{
			var actions = actionCandidates.OrderBy(x => x.Order);
			foreach(var action in actions)
			{
				if (this.actions.IndexOf(action) > -1) continue;
				if (await action.CanPerformAsync(this))
					this.actions.Add(action);
			}
		}

		public Guid Id { get; set; }

		public IDataSource Source { get; set; }
	}
}