namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
	using System.Linq;
	using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardImageDataControlFactory
        : IClipboardImageDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

		public int Priority => 3;

		public ClipboardImageDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardDataPackage data)
        {
            return data.Contents.Any(x => x is IClipboardImageData);
        }

        public IClipboardControl BuildControl(IClipboardDataPackage data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

			var imageData = data
				.Contents
				.OfType<IClipboardImageData>()
				.First();
			return CreateImageDataControl(imageData);
        }

        IClipboardControl CreateImageDataControl(IClipboardImageData data)
        {
            return new ClipboardImageDataControl
            {
                DataContext = new ClipboardImageDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}