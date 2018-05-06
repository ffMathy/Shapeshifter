namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using Data;

    using Properties;

    using Services.Interfaces;

    class DesignerClipboardImageDataFacade: ClipboardImageData
    {
        public DesignerClipboardImageDataFacade(
            IDesignerImageConverterService designerImageConverterService)
            :
                base()
        {
            RawData =
                designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(
                    Resources
                        .FileImageSample);
        }
    }
}