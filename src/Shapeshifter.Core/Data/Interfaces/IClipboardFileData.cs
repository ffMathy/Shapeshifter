using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Data.Interfaces
{
    public interface IClipboardFileData : IClipboardData
    {
        string FileName { get; set; }
        byte[] FileIcon { get; set; }
    }
}
