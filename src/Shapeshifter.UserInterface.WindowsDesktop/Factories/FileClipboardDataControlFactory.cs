using System;
using System.IO;
using System.Linq;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using System.Text;
using System.Collections.Generic;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class FileClipboardDataControlFactory : IFileClipboardDataControlFactory
    {
        readonly IDataSourceService dataSourceService;
        readonly IFileIconService fileIconService;
        readonly IMemoryHandleFactory memoryHandleFactory;

        readonly IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl> clipboardFileControlFactory;
        readonly IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl> clipboardFileCollectionControlFactory;

        public FileClipboardDataControlFactory(
            IDataSourceService dataSourceService,
            IFileIconService fileIconService,
            IMemoryHandleFactory memoryHandleFactory,
            IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl> clipboardFileControlFactory,
            IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl> clipboardFileCollectionControlFactory)
        {
            this.dataSourceService = dataSourceService;
            this.fileIconService = fileIconService;
            this.memoryHandleFactory = memoryHandleFactory;
            this.clipboardFileControlFactory = clipboardFileControlFactory;
            this.clipboardFileCollectionControlFactory = clipboardFileCollectionControlFactory;
        }

        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            if (clipboardData is IClipboardFileCollectionData)
            {
                return clipboardFileCollectionControlFactory.CreateControl((IClipboardFileCollectionData)clipboardData);
            }
            else if (clipboardData is IClipboardFileData)
            {
                return clipboardFileControlFactory.CreateControl((IClipboardFileData)clipboardData);
            }
            else
            {
                throw new ArgumentException("Unknown clipboard data type.", nameof(clipboardData));
            }
        }

        public IClipboardData BuildData(uint format, byte[] rawData)
        {
            if (!CanBuildData(format))
            {
                throw new ArgumentException("Can't construct data from this format.", nameof(format));
            }

            var files = GetFilesCopiedFromRawData(rawData);
            return ConstructDataFromFiles(files, format, rawData);
        }

        IEnumerable<string> GetFilesCopiedFromRawData(byte[] data)
        {
            var files = new List<string>();
            using (var memoryHandle = memoryHandleFactory.AllocateInMemory(data))
            {
                var count = ClipboardApi.DragQueryFile(memoryHandle.Pointer, uint.MaxValue, null, 0);
                for (var i = 0u; i < count; i++)
                {
                    var filenameBuilder = new StringBuilder(256);
                    ClipboardApi.DragQueryFile(memoryHandle.Pointer, i, filenameBuilder, filenameBuilder.Capacity);

                    files.Add(filenameBuilder.ToString());
                }
            }

            return files;
        }

        IClipboardData ConstructDataFromFiles(
            IEnumerable<string> files, uint format, byte[] rawData)
        {
            if (files.Count() == 1)
            {
                return ConstructClipboardFileData(
                    files.Single(), format, rawData);
            }
            else
            {
                return ConstructClipboardFileCollectionData(
                    files, format, rawData);
            }
        }

        IClipboardData ConstructClipboardFileCollectionData(
            IEnumerable<string> files, uint format, byte[] rawData)
        {
            return new ClipboardFileCollectionData(dataSourceService)
            {
                Files = files.Select(ConstructClipboardFileData),
                RawFormat = format,
                RawData = rawData
            };
        }

        IClipboardFileData ConstructClipboardFileData(
            string file, uint format, byte[] rawData)
        {
            return new ClipboardFileData(dataSourceService)
            {
                FileName = Path.GetFileName(file),
                FileIcon = fileIconService.GetIcon(file, false),
                RawFormat = format,
                RawData = rawData
            };
        }

        IClipboardFileData ConstructClipboardFileData(
            string file)
        {
            return ConstructClipboardFileData(file, 0, null);
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return data is IClipboardFileData || data is IClipboardFileCollectionData;
        }

        public bool CanBuildData(uint format)
        {
            return
                format == ClipboardApi.CF_HDROP;
        }
    }
}
