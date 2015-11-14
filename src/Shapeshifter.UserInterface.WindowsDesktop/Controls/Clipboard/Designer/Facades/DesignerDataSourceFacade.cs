using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    [ExcludeFromCodeCoverage]
    class DesignerDataSourceFacade : IDataSource
    {

        byte[] icon;

        readonly IDesignerImageConverterService designerImageConverterService;

        public DesignerDataSourceFacade(
            IDesignerImageConverterService designerImageConverterService)
        {
            this.designerImageConverterService = designerImageConverterService;
        }

        byte[] DecorateIcon(byte[] iconBytes)
        {
            return designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(iconBytes);
        }

        public byte[] Icon
        {
            get
            {
                return icon;
            }
            set
            {
                icon = DecorateIcon(value);
            }
        }

        public string Text
        {
            get; set;
        }
    }
}
