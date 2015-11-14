using System.Collections.Generic;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    public class ClipboardDataControlPackage : IClipboardDataControlPackage
    {
        private readonly IList<IClipboardData> dataCollection;

        public ClipboardDataControlPackage()
        {
            dataCollection = new List<IClipboardData>();
        }

        public IEnumerable<IClipboardData> Contents => dataCollection;

        public IClipboardControl Control { get; set; }

        public void AddData(IClipboardData data)
        {
            dataCollection.Add(data);
        }
    }
}