#region

using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    [ExcludeFromCodeCoverage]
    internal class DesignerClipboardImageDataFacade : ClipboardImageData
    {
        public DesignerClipboardImageDataFacade(
            IDesignerImageConverterService designerImageConverterService) :
                base(new DesignerFileDataSourceService(designerImageConverterService))
        {
            RawData = designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(Resources.FileImageSample);
        }
    }
}