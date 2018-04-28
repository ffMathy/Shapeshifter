namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
	using System.Linq;
	using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels.FileCollection;

    class ClipboardFileCollectionDataControlFactory
        : IClipboardFileCollectionDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

		public int Priority => 1;

		public ClipboardFileCollectionDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardDataPackage data)
        {
            return data.Contents.Any(x => x is IClipboardFileCollectionData);
        }

        public IClipboardControl BuildControl(
			IClipboardDataPackage data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

			var fileCollectionData = data
				.Contents
				.OfType<IClipboardFileCollectionData>()
				.First();
            return CreateClipboardFileCollectionDataControl(fileCollectionData);
        }

        IClipboardControl CreateClipboardFileCollectionDataControl(
            IClipboardFileCollectionData data)
        {
            return new ClipboardFileCollectionDataControl
            {
                DataContext = new ClipboardFileCollectionDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}