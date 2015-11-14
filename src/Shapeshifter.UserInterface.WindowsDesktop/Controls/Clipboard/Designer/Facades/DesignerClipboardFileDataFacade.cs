using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    [ExcludeFromCodeCoverage]
    internal class DesignerClipboardFileDataFacade : ClipboardFileData
    {
        public DesignerClipboardFileDataFacade(
            IDesignerImageConverterService designerImageConverterService) :
                base(new DesignerFileDataSourceService(designerImageConverterService))
        {
            FileName = "Kitten.jpg";
            FileIcon = designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(Resources.FileDataFileIcon);
        }
    }
}