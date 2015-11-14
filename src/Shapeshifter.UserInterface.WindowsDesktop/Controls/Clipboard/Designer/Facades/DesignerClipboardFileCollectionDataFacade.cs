using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    [ExcludeFromCodeCoverage]
    internal class DesignerClipboardFileCollectionDataFacade : ClipboardFileCollectionData
    {
        public DesignerClipboardFileCollectionDataFacade(
            IDesignerImageConverterService designerImageConverterService) :
                base(new DesignerFileDataSourceService(designerImageConverterService))
        {
            Files = new[]
            {
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.jpg"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.png"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.png"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.png"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.docx"},
                new DesignerClipboardFileDataFacade(designerImageConverterService) {FileName = "Foo.docx"}
            };
        }
    }
}