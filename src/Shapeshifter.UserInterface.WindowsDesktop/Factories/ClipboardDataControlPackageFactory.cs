#region

using System.Collections.Generic;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Unwrappers.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Threading.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    internal class ClipboardDataControlPackageFactory : IClipboardDataControlPackageFactory
    {
        private readonly IClipboardHandleFactory clipboardSessionFactory;
        private readonly IEnumerable<IMemoryUnwrapper> memoryUnwrappers;
        private readonly IEnumerable<IClipboardDataControlFactory> dataFactories;
        private readonly IUserInterfaceThread userInterfaceThread;

        public ClipboardDataControlPackageFactory(
            IEnumerable<IClipboardDataControlFactory> dataFactories,
            IEnumerable<IMemoryUnwrapper> memoryUnwrappers,
            IClipboardHandleFactory clipboardSessionFactory,
            IUserInterfaceThread userInterfaceThread)
        {
            this.dataFactories = dataFactories;
            this.memoryUnwrappers = memoryUnwrappers;
            this.clipboardSessionFactory = clipboardSessionFactory;
            this.userInterfaceThread = userInterfaceThread;
        }

        private bool IsAnyFormatSupported(
            IEnumerable<uint> formats)
        {
            return dataFactories.Any(
                x => formats.Any(x.CanBuildData));
        }

        public IClipboardDataControlPackage Create()
        {
            using (clipboardSessionFactory.StartNewSession())
            {
                var formats = ClipboardApi.GetClipboardFormats();
                if (IsAnyFormatSupported(formats))
                {
                    return ConstructPackage(formats);
                }
                return null;
            }
        }

        private IClipboardDataControlPackage ConstructPackage(IEnumerable<uint> formats)
        {
            var package = new ClipboardDataControlPackage();
            DecoratePackageWithClipboardData(formats, package);
            userInterfaceThread.Invoke(() => DecoratePackageWithControl(package));

            return package;
        }

        private void DecoratePackageWithClipboardData(
            IEnumerable<uint> formats,
            ClipboardDataControlPackage package)
        {
            foreach (var format in formats)
            {
                var dataFactory = dataFactories.FirstOrDefault(x => x.CanBuildData(format));
                if (dataFactory != null)
                {
                    DecoratePackageWithFormatDataUsingFactory(package, dataFactory, format);
                }
            }
        }

        private void DecoratePackageWithFormatDataUsingFactory(
            ClipboardDataControlPackage package, IClipboardDataControlFactory factory, uint format)
        {
            var unwrapper = memoryUnwrappers.FirstOrDefault(x => x.CanUnwrap(format));
            if (unwrapper != null)
            {
                var rawData = unwrapper.UnwrapStructure(format);
                if (rawData != null)
                {
                    var clipboardData = factory.BuildData(format, rawData);
                    if (clipboardData != null)
                    {
                        package.AddData(clipboardData);
                    }
                }
            }
        }

        private void DecoratePackageWithControl(ClipboardDataControlPackage package)
        {
            foreach (var data in package.Contents)
            {
                var dataFactory = dataFactories.FirstOrDefault(x => x.CanBuildControl(data));
                if (dataFactory != null)
                {
                    package.Control = dataFactory.BuildControl(data);
                    break;
                }
            }
        }
    }
}