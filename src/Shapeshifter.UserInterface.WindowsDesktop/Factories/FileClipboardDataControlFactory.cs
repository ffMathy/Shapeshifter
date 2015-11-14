using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Handles.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    internal class FileClipboardDataControlFactory : IFileClipboardDataControlFactory
    {
        private readonly IDataSourceService dataSourceService;
        private readonly IFileIconService fileIconService;
        private readonly IMemoryHandleFactory memoryHandleFactory;

        private readonly IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl>
            clipboardFileControlFactory;

        private readonly IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
            clipboardFileCollectionControlFactory;

        public FileClipboardDataControlFactory(
            IDataSourceService dataSourceService,
            IFileIconService fileIconService,
            IMemoryHandleFactory memoryHandleFactory,
            IClipboardControlFactory<IClipboardFileData, IClipboardFileDataControl> clipboardFileControlFactory,
            IClipboardControlFactory<IClipboardFileCollectionData, IClipboardFileCollectionDataControl>
                clipboardFileCollectionControlFactory)
        {
            this.dataSourceService = dataSourceService;
            this.fileIconService = fileIconService;
            this.memoryHandleFactory = memoryHandleFactory;
            this.clipboardFileControlFactory = clipboardFileControlFactory;
            this.clipboardFileCollectionControlFactory = clipboardFileCollectionControlFactory;
        }

        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            var clipboardFileCollectionData = clipboardData as IClipboardFileCollectionData;
            if (clipboardFileCollectionData != null)
            {
                return clipboardFileCollectionControlFactory.CreateControl(
                    clipboardFileCollectionData);
            }

            var clipboardFileData = clipboardData as IClipboardFileData;
            if (clipboardFileData != null)
            {
                return clipboardFileControlFactory.CreateControl(
                    clipboardFileData);
            }

            throw new ArgumentException(
                "Unknown clipboard data type.", nameof(clipboardData));
        }

        public IClipboardData BuildData(
            uint format, byte[] rawData)
        {
            if (!CanBuildData(format))
            {
                throw new ArgumentException(
                    "Can't construct data from this format.", nameof(format));
            }

            var files = GetFilesCopiedFromRawData(rawData);
            if (!files.Any())
            {
                return null;
            }

            return ConstructDataFromFiles(files, format, rawData);
        }

        private IReadOnlyCollection<string> GetFilesCopiedFromRawData(byte[] data)
        {
            var files = new List<string>();
            using (var memoryHandle = memoryHandleFactory.AllocateInMemory(data))
            {
                var count = ClipboardApi.DragQueryFile(memoryHandle.Pointer, 0xFFFFFFFF, null, 0);
                FetchFilesFromMemory(files, memoryHandle, count);
            }

            return files;
        }

        private static void FetchFilesFromMemory(List<string> files, IMemoryHandle memoryHandle, int count)
        {
            for (var i = 0u; i < count; i++)
            {
                var length = ClipboardApi.DragQueryFile(memoryHandle.Pointer, i, null, 0);
                var filenameBuilder = new StringBuilder(length);

                length = ClipboardApi.DragQueryFile(memoryHandle.Pointer, i, filenameBuilder, length + 1);

                var fileName = filenameBuilder.ToString(0, length);
                files.Add(fileName);
            }
        }

        private IClipboardData ConstructDataFromFiles(
            IReadOnlyCollection<string> files, uint format, byte[] rawData)
        {
            if (files.Count == 1)
            {
                return ConstructClipboardFileData(
                    files.Single(), format, rawData);
            }
            return ConstructClipboardFileCollectionData(
                files, format, rawData);
        }

        private IClipboardData ConstructClipboardFileCollectionData(
            IEnumerable<string> files, uint format, byte[] rawData)
        {
            return new ClipboardFileCollectionData(dataSourceService)
            {
                Files = files.Select(ConstructClipboardFileData).ToArray(),
                RawFormat = format,
                RawData = rawData
            };
        }

        private IClipboardFileData ConstructClipboardFileData(
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

        private IClipboardFileData ConstructClipboardFileData(
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