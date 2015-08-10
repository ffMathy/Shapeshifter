using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Data.Interfaces
{
    public interface IClipboardFileCollectionData : IClipboardData
    {
        IEnumerable<IClipboardFileData> Files { get; set; }
    }
}
