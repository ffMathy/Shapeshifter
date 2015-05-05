using System;
using System.Linq;
using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class FileClipboardDataControlFactory : IClipboardDataControlFactory
    {
        public UIElement BuildControl(IClipboardData clipboardData)
        {
            throw new NotImplementedException();
        }

        public IClipboardData BuildData(string format, object data)
        {
            var files = (string[])data;
            if(files.Length == 1)
            {
                return new ClipboardFileData()
                {
                    File = files[0]
                };
            } else
            {
                return new ClipboardFileCollectionData()
                {
                    Files = files.Select(x => new ClipboardFileData()
                    {
                        File = x
                    });
                };
            }
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
