using System.IO;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories
{
    class DesignerTextDataSourceFactory : IDataSourceFactory
    {
        public IDataSource GetDataSource()
        {
            return new DesignerDataSource()
            {
                Text = "Skype",
                Icon = Resources.TextDataSourceIcon
            };
        }
    }
}
