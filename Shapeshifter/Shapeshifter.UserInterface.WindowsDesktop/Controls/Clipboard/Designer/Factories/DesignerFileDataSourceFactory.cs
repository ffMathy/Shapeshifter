using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Factories
{
    class DesignerFileDataSourceFactory : IDataSourceFactory
    {
        public IDataSource GetDataSource()
        {
            return new DesignerDataSource()
            {
                Text = "My documents"
            };
        }
    }
}
