using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Properties;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    class DesignerDataSourceFacade : IDataSource
    {
        public DesignerDataSourceFacade()
        {
            Icon = Resources.TextDataSourceIcon;
            Text = "Skype";
        }

        public byte[] Icon
        {
            get;set;
        }

        public string Text
        {
            get; set;
        }
    }
}
