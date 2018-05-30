namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Clipboard.Interfaces;

	using Data;
	using Data.Factories.Interfaces;
	using Data.Interfaces;

	using Infrastructure.Threading.Interfaces;

	using Interfaces;

	class ClipboardDataControlPackageFactory : IClipboardDataControlPackageFactory
	{
		readonly IClipboardDataPackageFactory dataPackageFactory;
		readonly IMainThreadInvoker mainThreadInvoker;

		readonly IEnumerable<IClipboardDataControlFactory> controlFactories;

		public ClipboardDataControlPackageFactory(
			IClipboardDataPackageFactory dataPackageFactory,
			IEnumerable<IClipboardDataControlFactory> controlFactories,
			IMainThreadInvoker mainThreadInvoker)
		{
			this.dataPackageFactory = dataPackageFactory;
			this.controlFactories = controlFactories.OrderBy(x => x.Priority);
			this.mainThreadInvoker = mainThreadInvoker;
		}

		public async Task<IClipboardDataControlPackage> CreateFromCurrentClipboardDataAsync()
		{
			var dataPackage = await dataPackageFactory.CreateFromCurrentClipboardDataAsync();			
			return CreateFromDataPackage(dataPackage);
		}

		ClipboardDataControlPackage CreateDataControlPackageFromDataPackage(IClipboardDataPackage dataPackage)
		{
			var control = CreateControlFromDataPackage(dataPackage);
			if (control == null)
				return null;

			var package = new ClipboardDataControlPackage(dataPackage, control);
			return package;
		}

		public IClipboardDataControlPackage CreateFromDataPackage(IClipboardDataPackage dataPackage)
		{
			ClipboardDataControlPackage package = null;
			mainThreadInvoker.InvokeOnWindowsPresentationFoundationThread(
				() => package = CreateDataControlPackageFromDataPackage(dataPackage));

			return package;
		}

		IClipboardControl CreateControlFromDataPackage(IClipboardDataPackage dataPackage)
		{
			if(dataPackage == null)
				return null;
		
			var matchingFactory = controlFactories.FirstOrDefault(x => x.CanBuildControl(dataPackage));
			if (matchingFactory != null)
				return matchingFactory.BuildControl(dataPackage);

			return null;
		}
	}
}
