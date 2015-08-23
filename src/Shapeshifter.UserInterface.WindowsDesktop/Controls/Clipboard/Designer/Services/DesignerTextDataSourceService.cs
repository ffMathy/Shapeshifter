using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories
{
    [ExcludeFromCodeCoverage]
    class DesignerTextDataSourceService : IDataSourceService
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
