namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    using System.Collections.Generic;

    using Controls.Clipboard.Interfaces;

    using Interfaces;

    public class ClipboardDataControlPackage: IClipboardDataControlPackage
    {
        readonly IList<IClipboardData> dataCollection;

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