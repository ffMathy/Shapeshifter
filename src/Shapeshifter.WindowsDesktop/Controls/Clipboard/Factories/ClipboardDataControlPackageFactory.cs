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
    using Exceptions;

    class ClipboardDataControlPackageFactory : IClipboardDataControlPackageFactory
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
            try
            {
                var dataPackage = dataPackageFactory.CreateFromCurrentClipboardData();
                return CreateFromDataPackage(dataPackage);
            }
            catch (ClipboardFormatNotUnderstoodException)
            {
                //TODO: #20 - adding support for custom data
                return null;
            }
        }

        ClipboardDataControlPackage CreateDataControlPackageFromDataPackage(IClipboardDataPackage dataPackage)
        {
            var control = CreateControlFromDataPackage(dataPackage);
            if (control == null)
            {
                return null;
            }

            var package = new ClipboardDataControlPackage(dataPackage, control);
            return package;
        }

        public IClipboardDataControlPackage CreateFromDataPackage(IClipboardDataPackage dataPackage)
        {
            ClipboardDataControlPackage package = null;
            mainThreadInvoker.Invoke(
                () => package = CreateDataControlPackageFromDataPackage(dataPackage));

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