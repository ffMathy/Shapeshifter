using System.Collections;
using System.Collections.Generic;
using System.Windows;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IClipboardManagementService
    {
        IEnumerable<IClipboardControlDataPackage> ClipboardElements { get; }
    }
}
