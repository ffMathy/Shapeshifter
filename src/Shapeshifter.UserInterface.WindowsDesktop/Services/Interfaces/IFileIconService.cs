using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    interface IFileIconService
    {
        byte[] GetIcon(string path, bool allowThumbnails, int dimensions = 256);
    }
}
