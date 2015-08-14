using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerClipboardFileDataFacade : ClipboardFileData
    {
        public DesignerClipboardFileDataFacade(
            IDesignerImageConverterService designerImageConverterService) : 
            base(new DesignerFileDataSourceService())
        {
            FileName = "Kitten.jpg";
            FileIcon = designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(Resources.FileDataFileIcon);
        }

        private void ConfigureIcon()
        {
        }
    }
}
