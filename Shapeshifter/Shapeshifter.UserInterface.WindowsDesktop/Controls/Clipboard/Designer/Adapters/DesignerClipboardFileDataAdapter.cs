using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileDataAdapter : ClipboardFileData
    {
        public DesignerClipboardFileDataAdapter() : 
            base(new DesignerFileDataSourceFactory())
        {
        }
    }
}
