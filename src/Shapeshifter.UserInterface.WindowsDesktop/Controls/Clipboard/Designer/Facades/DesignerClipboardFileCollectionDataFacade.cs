using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileCollectionDataFacade : ClipboardFileCollectionData
    {
        public DesignerClipboardFileCollectionDataFacade(IDesignerImageConverterService designerImageConverterService) :
            base(new DesignerFileDataSourceService())
        {
            Files = new[] {
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.png" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.png" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.png" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.docx" },
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.docx" }
            };
        }
    }
}
