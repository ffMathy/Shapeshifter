namespace Shapeshifter.UserInterface.WindowsDesktop.Data
{
    using System.Collections.Generic;

    using Interfaces;

    public class ClipboardDataPackage: IClipboardDataPackage
    {
        readonly List<IClipboardData> dataCollection;

        public ClipboardDataPackage()
        {
            dataCollection = new List<IClipboardData>();
        }

        public IReadOnlyList<IClipboardData> Contents => 
            dataCollection;

        public void AddData(IClipboardData data)
        {
            dataCollection.Add(data);
        }
    }
}