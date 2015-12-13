namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories
{
    using System.Collections.Generic;
    using System.Linq;

    using Clipboard.Interfaces;

    using Data;
    using Data.Factories.Interfaces;
    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    class ClipboardDataControlPackageFactory: IClipboardDataControlPackageFactory
    {
        readonly IClipboardDataPackageFactory dataPackageFactory;

        readonly IEnumerable<IClipboardDataControlFactory> controlFactories;

        readonly IMainThreadInvoker mainThreadInvoker;

        public ClipboardDataControlPackageFactory(
            IClipboardDataPackageFactory dataPackageFactory,
            IEnumerable<IClipboardDataControlFactory> controlFactories,
            IMainThreadInvoker mainThreadInvoker)
        {
            this.dataPackageFactory = dataPackageFactory;
            this.controlFactories = controlFactories;
            this.mainThreadInvoker = mainThreadInvoker;
        }

        public IClipboardDataControlPackage CreateFromCurrentClipboardData()
        {
            var dataPackage = dataPackageFactory.CreateFromCurrentClipboardData();
            if (dataPackage == null)
            {
                return null;
            }

            ClipboardDataControlPackage package = null;
            mainThreadInvoker.Invoke(
                () => {
                    var control = CreateControlFromDataPackage(dataPackage);
                    if (control == null)
                    {
                        return;
                    }

                    package = new ClipboardDataControlPackage(dataPackage, control);
                });

            return package;
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