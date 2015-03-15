using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Core
{
    public interface IClipboardItemControl<TControlType>
    {
        TControlType Header { get; set; }
        TControlType Body { get; set; }
        TControlType Source { get; set; }
        TControlType BackgroundImage { get; set; }
    }
}
