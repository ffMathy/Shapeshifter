using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileCollectionDataFacade : ClipboardFileCollectionData
    {
        public DesignerClipboardFileCollectionDataFacade(IImagePersistenceService imagePersistenceService) :
            base(new DesignerFileDataSourceService(imagePersistenceService))
        {
            Files = new[] {
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.jpg" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.png" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.png" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.png" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.docx" },
                new DesignerClipboardFileDataFacade(imagePersistenceService) {FileName = "Foo.docx" }
            };
        }
    }
}
