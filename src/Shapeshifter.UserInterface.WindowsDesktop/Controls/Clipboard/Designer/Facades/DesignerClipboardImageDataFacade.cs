namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using System.Diagnostics.CodeAnalysis;

    using Data;

    using Properties;

    using Services;
    using Services.Interfaces;

    [ExcludeFromCodeCoverage]
    class DesignerClipboardImageDataFacade: ClipboardImageData
    {
        public DesignerClipboardImageDataFacade(
            IDesignerImageConverterService designerImageConverterService)
            :
                base(new DesignerFileDataSourceService(designerImageConverterService))
        {
            RawData =
                designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(
                                                                                      Resources
                                                                                          .FileImageSample);
        }
    }
}