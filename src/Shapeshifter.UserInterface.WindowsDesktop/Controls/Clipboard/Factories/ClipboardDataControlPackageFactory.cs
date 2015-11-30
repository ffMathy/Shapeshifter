namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories
{
    using System.Collections.Generic;
    using System.Linq;

    using Clipboard.Interfaces;

    using Data;
    using Data.Factories.Interfaces;
    using Data.Interfaces;

    using Interfaces;

    class ClipboardDataControlPackageFactory: IClipboardDataControlPackageFactory
    {
        readonly IClipboardDataPackageFactory dataPackageFactory;

        readonly IEnumerable<IClipboardDataControlFactory> controlFactories;

        public ClipboardDataControlPackageFactory(
            IClipboardDataPackageFactory dataPackageFactory,
            IEnumerable<IClipboardDataControlFactory> controlFactories)
        {
            this.dataPackageFactory = dataPackageFactory;
            this.controlFactories = controlFactories;
        }

        public IClipboardDataControlPackage CreateFromCurrentClipboardData()
        {
            var dataPackage = dataPackageFactory.CreateFromCurrentClipboardData();
            if (dataPackage == null)
            {
                return null;
            }

            var control = CreateControlFromDataPackage(dataPackage);
            if (control == null)
            {
                return null;
            }

            return new ClipboardDataControlPackage(dataPackage, control);
        }

        IClipboardControl CreateControlFromDataPackage(IClipboardDataPackage dataPackage)
        {
            foreach (var item in dataPackage.Contents)
            {
                var matchingFactory = controlFactories.FirstOrDefault(x => x.CanBuildControl(item));
                if (matchingFactory != null)
                {
                    return matchingFactory.BuildControl(item);
                }
            }

            return null;
        }
    }
}