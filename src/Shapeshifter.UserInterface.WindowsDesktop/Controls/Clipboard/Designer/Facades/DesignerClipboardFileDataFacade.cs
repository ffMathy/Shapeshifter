namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using System.Diagnostics.CodeAnalysis;

    using Data;

    using Properties;

    using Services;
    using Services.Interfaces;

    [ExcludeFromCodeCoverage]
    class DesignerClipboardFileDataFacade: ClipboardFileData
    {
        public DesignerClipboardFileDataFacade(
            IDesignerImageConverterService designerImageConverterService)
            :
                base(new DesignerFileDataSourceService(designerImageConverterService))
        {
            FileName = "Kitten.jpg";
            FileIcon =
                designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(
                                                                                      Resources
                                                                                          .FileDataFileIcon);
        }
    }
}