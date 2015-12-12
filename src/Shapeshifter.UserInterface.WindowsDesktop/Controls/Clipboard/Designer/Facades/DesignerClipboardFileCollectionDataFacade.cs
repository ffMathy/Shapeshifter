namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using Data;

    using Services;
    using Services.Interfaces;

    
    class DesignerClipboardFileCollectionDataFacade: ClipboardFileCollectionData
    {
        public DesignerClipboardFileCollectionDataFacade(
            IDesignerImageConverterService designerImageConverterService)
            :
                base(new DesignerFileDataSourceService(designerImageConverterService))
        {
            Files = new[]
            {
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.jpg"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.jpg"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.jpg"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.jpg"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.png"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.png"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.png"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.docx"
                },
                new DesignerClipboardFileDataFacade(designerImageConverterService)
                {
                    FileName = "Foo.docx"
                }
            };
        }
    }
}