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
            return ConstructDataFromFiles(files);
        }

        IEnumerable<string> GetFilesCopiedFromRawData(byte[] data)
        {
            using (var memoryHandle = memoryHandleFactory.AllocateInMemory(data))
            {
                var count = ClipboardApi.DragQueryFile(memoryHandle.Pointer, uint.MaxValue, null, 0);
                for (var i = 0u; i < count; i++)
                {
                    var filenameBuilder = new StringBuilder(256);
                    ClipboardApi.DragQueryFile(memoryHandle.Pointer, i, filenameBuilder, filenameBuilder.Capacity);

                    yield return filenameBuilder.ToString();
                }
            }
        }

        IClipboardData ConstructDataFromFiles(IEnumerable<string> files)
        {
            if (files.Count() == 1)
            {
                return ConstructClipboardFileData(files.Single());
            }
            else
            {
                return ConstructClipboardFileCollectionData(files);
            }
        }

        IClipboardData ConstructClipboardFileCollectionData(IEnumerable<string> files)
        {
            return new ClipboardFileCollectionData(dataSourceService)
            {
                Files = files.Select(ConstructClipboardFileData)
            };
        }

        IClipboardFileData ConstructClipboardFileData(string file)
        {
            return new ClipboardFileData(dataSourceService)
            {
                FileName = Path.GetFileName(file),
                FileIcon = fileIconService.GetIcon(file, false)
            };
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
