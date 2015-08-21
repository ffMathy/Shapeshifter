using System.Collections.Generic;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Core.Data
{
    public class ClipboardDataControlPackage : IClipboardDataControlPackage
    {
        private IList<IClipboardData> data;

        public ClipboardDataControlPackage()
        {
            data = new List<IClipboardData>();
        }

        public IEnumerable<IClipboardData> Contents
        {
            get
            {
                return data;
            }
        }

        public IClipboardControl Control
        {
            get;set;
        }

        public void AddData(IClipboardData data)
        {
            this.data.Add(data);
        }
    }
}
