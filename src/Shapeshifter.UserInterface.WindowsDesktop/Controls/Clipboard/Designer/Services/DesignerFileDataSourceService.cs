namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using WindowsDesktop.Services.Interfaces;

    using Data.Interfaces;

    using Facades;

    using Interfaces;

    using Properties;

    
    class DesignerFileDataSourceService
        : IDataSourceService,
          IDesignerService
    {
        readonly IDesignerImageConverterService designerImageConverterService;

        public DesignerFileDataSourceService(
            IDesignerImageConverterService designerImageConverterService)
        {
            this.designerImageConverterService = designerImageConverterService;
        }

        public IDataSource GetDataSource()
        {
            return new DesignerDataSourceFacade(designerImageConverterService)
            {
                Text = "My pictures",
                Icon = Resources.FileDataSourceIcon
            };
        }
    }
}