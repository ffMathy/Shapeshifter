namespace Shapeshifter.WindowsDesktop.Data.Factories
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Data.Interfaces;

    using Infrastructure.Handles.Factories.Interfaces;
    using Infrastructure.Handles.Interfaces;

    using Interfaces;

    using Native;
    using Native.Interfaces;

    using Services.Files.Interfaces;

    class FileClipboardDataFactory: IFileClipboardDataFactory
    {
        readonly IFileIconService fileIconService;
        readonly IMemoryHandleFactory memoryHandleFactory;
        readonly IClipboardNativeApi clipboardNativeApi;

        public FileClipboardDataFactory(
            IFileIconService fileIconService,
            IMemoryHandleFactory memoryHandleFactory,
            IClipboardNativeApi clipboardNativeApi)
        {
            this.fileIconService = fileIconService;
            this.memoryHandleFactory = memoryHandleFactory;
            this.clipboardNativeApi = clipboardNativeApi;
        }

        public IClipboardData BuildData(
			IClipboardFormat format,
            byte[] rawData)
        {
            if (!CanBuildData(format))
            {
                throw new ArgumentException(
                    "Can't construct data from this format.",
                    nameof(format));
            }

            var files = GetFilesCopiedFromRawData(rawData);
            if (!files.Any())
            {
                return null;
            }

            return ConstructDataFromFiles(files, format, rawData);
        }

        IReadOnlyCollection<string> GetFilesCopiedFromRawData(byte[] data)
        {
            var files = new List<string>();
            using (var memoryHandle = memoryHandleFactory.AllocateInMemory(data))
            {
                var count = clipboardNativeApi.DragQueryFile(memoryHandle.Pointer, 0xFFFFFFFF, null, 0);
                FetchFilesFromMemory(files, memoryHandle, count);
            }

            return files;
        }

        void FetchFilesFromMemory(
            ICollection<string> files,
            IMemoryHandle memoryHandle,
            int count)
        {
            for (var i = 0u; i < count; i++)
            {
                var length = clipboardNativeApi.DragQueryFile(memoryHandle.Pointer, i, null, 0);
                var filenameBuilder = new StringBuilder(length);

                length = clipboardNativeApi.DragQueryFile(
                    memoryHandle.Pointer,
                    i,
                    filenameBuilder,
                    length + 1);

                var fileName = filenameBuilder.ToString(0, length);
                files.Add(fileName);
            }
        }

        IClipboardData ConstructDataFromFiles(
            IReadOnlyCollection<string> files,
			IClipboardFormat format,
            byte[] rawData)
        {
            if (files.Count == 1)
            {
                return ConstructClipboardFileData(
                    files.Single(),
                    format,
                    rawData);
            }

            return ConstructClipboardFileCollectionData(
                files,
                format,
                rawData);
        }

        IClipboardData ConstructClipboardFileCollectionData(
            IEnumerable<string> files,
			IClipboardFormat format,
            byte[] rawData)
        {
            return new ClipboardFileCollectionData()
            {
                Files = files
					.Select(x => ConstructClipboardFileData(x, format))
                    .ToArray(),
                RawFormat = format,
                RawData = rawData
            };
        }

        IClipboardFileData ConstructClipboardFileData(
            string file,
			IClipboardFormat format,
            byte[] rawData)
        {
            return new ClipboardFileData()
            {
                FileName = Path.GetFileName(file),
                FullPath = file,
                FileIcon = fileIconService.GetIcon(file, false),
                RawFormat = format,
                RawData = rawData
            };
        }

        IClipboardFileData ConstructClipboardFileData(
            string file,
			IClipboardFormat format)
        {
            return ConstructClipboardFileData(file, format, null);
        }

        public bool CanBuildData(IClipboardFormat format)
        {
            return format.Number == ClipboardNativeApi.CF_HDROP;
        }
    }
}