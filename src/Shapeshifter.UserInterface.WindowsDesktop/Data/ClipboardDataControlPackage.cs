using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardDataControlPackage : IClipboardDataControlPackage
    {
        IList<IClipboardData> data;

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
