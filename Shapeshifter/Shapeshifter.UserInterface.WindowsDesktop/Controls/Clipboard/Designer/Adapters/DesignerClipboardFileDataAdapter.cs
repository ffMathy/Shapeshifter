using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileDataAdapter : ClipboardFileData
    {
        public DesignerClipboardFileDataAdapter() : 
            base(new DesignerFileDataSourceFactory())
        {
            FileName = "Kitten and chicks.jpg";
            FileIcon = Resources.FileDataFileIcon;
        }
    }
}
