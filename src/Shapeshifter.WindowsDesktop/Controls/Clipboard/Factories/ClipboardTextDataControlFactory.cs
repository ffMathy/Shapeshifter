namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
	using System.Linq;
	using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardTextDataControlFactory:
        IClipboardTextDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

		public int Priority => 4;

		public ClipboardTextDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardDataPackage data)
        {
            return data.Contents.Any(x => x is IClipboardTextData);
        }

        public IClipboardControl BuildControl(IClipboardDataPackage data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

			var textData = data
				.Contents
				.OfType<IClipboardTextData>()
				.First();
			return CreateClipboardTextDataControl(textData);
        }

        IClipboardControl CreateClipboardTextDataControl(IClipboardTextData data)
        {
            return new ClipboardTextDataControl
            {
                DataContext = new ClipboardTextDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}