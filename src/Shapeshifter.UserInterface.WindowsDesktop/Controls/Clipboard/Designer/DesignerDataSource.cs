using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer
{
    class DesignerDataSource : IDataSource
    {
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
