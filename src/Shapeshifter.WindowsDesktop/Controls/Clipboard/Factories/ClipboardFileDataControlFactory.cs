namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System;
	using System.Linq;
	using Clipboard.Interfaces;

    using Data.Interfaces;

    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    using ViewModels;

    class ClipboardFileDataControlFactory
        : IClipboardFileDataControlFactory
    {
        readonly IEnvironmentInformation environmentInformation;

		public int Priority => 2;

		public ClipboardFileDataControlFactory(
            IEnvironmentInformation environmentInformation)
        {
            this.environmentInformation = environmentInformation;
        }

        public bool CanBuildControl(IClipboardDataPackage data)
        {
            return data.Contents.Any(x => x is IClipboardFileData);
        }

        public IClipboardControl BuildControl(IClipboardDataPackage data)
        {
            if (data == null)
            {
                throw new ArgumentException(
                    "Data must be set when constructing a clipboard control.",
                    nameof(data));
            }

			var fileData = data
				.Contents
				.OfType<IClipboardFileData>()
				.First();
			return CreateFileDataControl(fileData);
        }

        IClipboardControl CreateFileDataControl(IClipboardFileData data)
        {
            return new ClipboardFileDataControl
            {
                DataContext = new ClipboardFileDataViewModel(environmentInformation)
                {
                    Data = data
                }
            };
        }
    }
}