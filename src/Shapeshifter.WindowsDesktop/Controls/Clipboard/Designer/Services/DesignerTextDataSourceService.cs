namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using Controls.Designer.Services;
    using Data.Interfaces;
    using Facades;
    using Interfaces;
    using Properties;
    using WindowsDesktop.Services.Clipboard.Interfaces;

    class DesignerTextDataSourceService
        : IDataSourceService,
          IDesignerService
    {
        readonly IDesignerImageConverterService designerImageConverterService;

        public DesignerTextDataSourceService(
            IDesignerImageConverterService designerImageConverterService)
        {
            this.designerImageConverterService = designerImageConverterService;
        }

        public IDataSource GetDataSource()
        {
            return new DesignerDataSourceFacade(designerImageConverterService)
            {
                Title = "Skype",
                IconLarge = Resources.TextDataSourceIcon,
                IconSmall = Resources.TextDataSourceIcon
            };
        }
    }
}