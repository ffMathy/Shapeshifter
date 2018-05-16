namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using Data.Interfaces;

    using Services.Interfaces;

    class DesignerDataSourceFacade : IDataSource
    {
        byte[] iconLarge;
        byte[] iconSmall;

        readonly IDesignerImageConverterService designerImageConverterService;

        public DesignerDataSourceFacade(IDesignerImageConverterService designerImageConverterService)
        {
            this.designerImageConverterService = designerImageConverterService;
        }

        byte[] DecorateIcon(byte[] iconBytes)
        {
            return designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(iconBytes);
        }

        public byte[] IconLarge
        {
            get
            {
                return iconLarge;
            }
            set
            {
                iconLarge = DecorateIcon(value);
            }
        }

        public byte[] IconSmall
        {
            get
            {
                return iconSmall;
            }
            set
            {
                iconSmall = DecorateIcon(value);
            }
        }

        public string Title { get; set; }
    }
}