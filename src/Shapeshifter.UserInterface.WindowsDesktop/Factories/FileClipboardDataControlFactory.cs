using System;
using System.IO;
using System.Linq;
using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.Core.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class FileClipboardDataControlFactory : IClipboardDataControlFactory
    {
        private readonly IDataSourceService dataSourceService;
        private readonly IFileIconService fileIconService;

        public FileClipboardDataControlFactory(
            IDataSourceService dataSourceService, 
            IFileIconService fileIconService)
        {
            this.dataSourceService = dataSourceService;
            this.fileIconService = fileIconService;
        }

        public UIElement BuildControl(IClipboardData clipboardData)
        {
            if(clipboardData is IClipboardFileCollectionData)
            {
                return CreateFileCollectionControl(clipboardData);
            }
            else if(clipboardData is IClipboardFileData)
            {
                return CreateFileControl(clipboardData);
            }
            else
            {
                throw new ArgumentException("Unknown clipboard data type.", nameof(clipboardData));
            }
        }

        private static UIElement CreateFileControl(IClipboardData clipboardData)
        {
            return new ClipboardFileDataControl()
            {
                DataContext = new ClipboardFileDataViewModel()
                {
                    Data = (IClipboardFileData)clipboardData
                }
            };
        }

        private static UIElement CreateFileCollectionControl(IClipboardData clipboardData)
        {
            return new ClipboardFileCollectionDataControl()
            {
                DataContext = new ClipboardFileCollectionDataViewModel()
                {
                    Data = (IClipboardFileCollectionData)clipboardData
                }
            };
        }

        public IClipboardData BuildData(string format, object data)
        {
            if (!CanBuildData(format))
            {
                throw new ArgumentException("Can't construct data from this format.", nameof(data));
            }

            var files = (string[])data;
            return ConstructDataFromFiles(files);
        }

        private IClipboardData ConstructDataFromFiles(string[] files)
        {
            if (files.Length == 1)
            {
                return ConstructClipboardFileData(files[0]);
            }
            else
            {
                return ConstructClipboardFileCollectionData(files);
            }
        }

        private IClipboardData ConstructClipboardFileCollectionData(string[] files)
        {
            return new ClipboardFileCollectionData(dataSourceService)
            {
                Files = files.Select(ConstructClipboardFileData)
            };
        }

        private ClipboardFileData ConstructClipboardFileData(string file)
        {
            return new ClipboardFileData(dataSourceService)
            {
                FileName = Path.GetFileName(file),
                FileIcon = fileIconService.GetIcon(file, false)
            };
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return data is ClipboardFileData || data is ClipboardFileCollectionData;
        }

        public bool CanBuildData(string format)
        {
            return format == DataFormats.FileDrop;
        }
    }
}
