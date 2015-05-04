using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces
{
    interface IClipboardControlDataPackage
    {
        void AddData(IClipboardData data);

        IEnumerable<IClipboardData> Contents { get; }

        UIElement Control { get; }
    }
}
