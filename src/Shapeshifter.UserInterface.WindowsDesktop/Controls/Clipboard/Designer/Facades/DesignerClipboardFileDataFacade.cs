using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileDataFacade : ClipboardFileData
    {
        public DesignerClipboardFileDataFacade(string extension = ".jpg") : 
            base(new DesignerFileDataSourceService())
        {
            FileName = "Kitten and chicks" + extension;
            FileIcon = Resources.FileDataFileIcon;
        }
    }
}
