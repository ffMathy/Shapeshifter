namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using WindowsDesktop.Services.Interfaces;

    using Data.Interfaces;

    using Facades;

    using Interfaces;

    using Properties;

    using Shared.Controls.Designer.Services;

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
                Text = "Skype",
                Icon = Resources.TextDataSourceIcon
            };
        }
    }
}