using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories
{
    class DesignerTextDataSourceService : IDataSourceService
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public DesignerTextDataSourceService(IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        public IDataSource GetDataSource()
        {
            return new DesignerDataSourceFacade(imagePersistenceService)
            {
                Text = "Skype",
                Icon = Resources.TextDataSourceIcon
            };
        }
    }
}
