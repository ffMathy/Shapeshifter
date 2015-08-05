using System.Collections.Generic;
using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Core.Data
{
    public class ClipboardDataControlPackage : IClipboardControlDataPackage
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

        public UIElement Control
        {
            get;set;
        }

        public void AddData(IClipboardData data)
        {
            this.data.Add(data);
        }
    }
}
