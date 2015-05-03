using System;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileCollectionDataFacade : ClipboardFileCollectionData
    {
        public DesignerClipboardFileCollectionDataFacade() :
            base(new DesignerFileDataSourceService())
        {
            Files = new[] {
                new DesignerClipboardFileDataFacade(".jpg"),
                new DesignerClipboardFileDataFacade(".jpg"),
                new DesignerClipboardFileDataFacade(".jpg"),
                new DesignerClipboardFileDataFacade(".jpg"),
                new DesignerClipboardFileDataFacade(".png"),
                new DesignerClipboardFileDataFacade(".png"),
                new DesignerClipboardFileDataFacade(".svg")
            };
        }
    }
}
