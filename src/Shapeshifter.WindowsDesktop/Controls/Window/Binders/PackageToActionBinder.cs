namespace Shapeshifter.WindowsDesktop.Controls.Window.Binders
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.Threading;
	using System.Threading.Tasks;

	using Data.Interfaces;

	using Interfaces;

	using ViewModels.Interfaces;

	class PackageToActionBinder : IAsyncListDictionaryBinder<IClipboardDataControlPackage, IActionViewModel>
	{
		readonly IDictionary<IClipboardDataControlPackage, ICollection<IActionViewModel>> dictionaryStates;

		readonly SemaphoreSlim collectionChangedLock;

		IClipboardDataControlPackage currentKey;

		ObservableCollection<IActionViewModel> boundDestinationCollection;

		Func<IClipboardDataControlPackage, Task<IEnumerable<IActionViewModel>>> currentMappingFunction;

		public IActionViewModel Default { get; set; }

		public PackageToActionBinder()
		{
			dictionaryStates = new Dictionary<IClipboardDataControlPackage, ICollection<IActionViewModel>>();
			collectionChangedLock = new SemaphoreSlim(1);
		}

		public void Bind(
			ObservableCollection<IClipboardDataControlPackage> sourceCollection,
			ObservableCollection<IActionViewModel> destinationCollection,
			Func<IClipboardDataControlPackage, Task<IEnumerable<IActionViewModel>>> mappingFunction)
		{
			this.boundDestinationCollection = destinationCollection;
			this.currentMappingFunction = mappingFunction;

			sourceCollection.CollectionChanged += SourceCollection_CollectionChanged;
		}

		public async void LoadFromKey(IClipboardDataControlPackage key)
		{
			await collectionChangedLock.WaitAsync();

			try
			{
				boundDestinationCollection.Clear();
				boundDestinationCollection.Add(Default);

				this.currentKey = key;
				foreach (var item in dictionaryStates[key])
				{
					boundDestinationCollection.Add(item);
				}
			}
			finally
			{
				collectionChangedLock.Release();
			}
		}

		async void SourceCollection_CollectionChanged(
			object sender,
			NotifyCollectionChangedEventArgs e)
		{
			if (e?.NewItems == null || e.NewItems.Count <= 0)
				return;

			if (e?.OldItems != null && e.OldItems.Count > 0)
				return;

			await collectionChangedLock.WaitAsync();
			try
			{
				foreach (IClipboardDataControlPackage item in e.NewItems)
				{
					PrepareDictionaryStateKey(item);
					await AddResultsToDictionary(item);
				}
			}
			finally
			{
				collectionChangedLock.Release();
			}
		}

		async Task AddResultsToDictionary(IClipboardDataControlPackage item)
		{
			var collection = dictionaryStates[item];
			var results = await currentMappingFunction(item);
			foreach (var result in results)
			{
				collection.Add(result);
				if (currentKey == item)
				{
					boundDestinationCollection.Add(result);
				}
			}
		}

		void PrepareDictionaryStateKey(IClipboardDataControlPackage item)
		{
			if (!dictionaryStates.ContainsKey(item))
				dictionaryStates.Add(item, new HashSet<IActionViewModel>());
		}
	}
}