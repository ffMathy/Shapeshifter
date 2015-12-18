namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using System.Windows.Forms;

    using Data;

    using Properties;

    using Services;
    using Services.Interfaces;

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