using Autofac;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    class DesignerDataSourceFacade : IDataSource
    {

        private byte[] icon;

        private readonly IDesignerImageConverterService designerImageConverterService;

        public DesignerDataSourceFacade()
        {
            this.designerImageConverterService = App.Container.Resolve<IDesignerImageConverterService>();
        }

        private byte[] DecorateIcon(byte[] iconBytes)
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
