using System;
using System.IO;
using System.Linq;
using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class FileClipboardDataControlFactory : IClipboardDataControlFactory
    {
        private readonly IDataSourceService dataSourceService;

        public FileClipboardDataControlFactory(IDataSourceService dataSourceService)
        {
            this.dataSourceService = dataSourceService;
        }

        public UIElement BuildControl(IClipboardData clipboardData)
        {
            if(clipboardData is ClipboardFileCollectionData)
            {
                return new ClipboardFileCollectionDataControl()
                {
                    DataContext = new ClipboardFileCollectionDataViewModel((ClipboardFileCollectionData)clipboardData)
                };
            } else if(clipboardData is ClipboardFileData)
            {
                return new ClipboardFileDataControl()
                {
                    DataContext = new ClipboardFileDataViewModel((ClipboardFileData)clipboardData)
                };
            } else
            {
                throw new ArgumentException("Unknown clipboard data type.", nameof(clipboardData));
            }
        }

        public IClipboardData BuildData(string format, object data)
        {
            if(!CanBuildData(format))
            {
                throw new ArgumentException("Can't construct data from this format.", nameof(data));
            }

            var files = (string[])data;
            if(files.Length == 1)
            {
                return ConstructClipboardFileData(files[0]);
            }
            else
            {
                return new ClipboardFileCollectionData(dataSourceService)
                {
                    Files = files.Select(ConstructClipboardFileData)
                };
            }
        }

        private ClipboardFileData ConstructClipboardFileData(string file)
        {
            return new ClipboardFileData(dataSourceService)
            {
                FileName = Path.GetFileName(file)
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
