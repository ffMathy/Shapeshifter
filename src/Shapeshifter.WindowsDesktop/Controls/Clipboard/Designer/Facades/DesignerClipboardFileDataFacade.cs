namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using Data;

    using Properties;

    using Services;
    using Services.Interfaces;

    class DesignerClipboardFileDataFacade: ClipboardFileData
    {
        public DesignerClipboardFileDataFacade(
            IDesignerImageConverterService designerImageConverterService)
            :
                base()
        {
            FileName = "Kitten.jpg";
            FileIcon =
                designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(
                    Resources
                        .FileDataFileIcon);
        }
    }
}