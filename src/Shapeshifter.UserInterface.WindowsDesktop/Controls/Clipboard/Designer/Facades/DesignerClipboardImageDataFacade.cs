using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    [ExcludeFromCodeCoverage]
    class DesignerClipboardImageDataFacade : ClipboardImageData
    {
        public DesignerClipboardImageDataFacade(
            IDesignerImageConverterService designerImageConverterService) : 
            base(new DesignerFileDataSourceService(designerImageConverterService))
        {
            RawData = designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(Resources.FileImageSample);
        }
    }
}
