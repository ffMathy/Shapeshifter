using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    [ExcludeFromCodeCoverage]
    internal class DesignerClipboardTextDataFacade : ClipboardTextData, IClipboardTextData
    {
        [ExcludeFromCodeCoverage]
        public DesignerClipboardTextDataFacade(IDesignerImageConverterService designerImageConverterService) :
            base(new DesignerTextDataSourceService(designerImageConverterService))
        {
            Text =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed eu purus vehicula, tincidunt velit eget, varius quam. Duis sollicitudin ultrices ipsum, et mollis tellus convallis vitae. Proin lobortis sapien eget varius imperdiet. In hac habitasse platea dictumst. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.";
        }
    }
}