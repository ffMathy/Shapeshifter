using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Data
{
    public interface IDataSource
    {
        byte[] Icon { get; }

        string Text { get; }
    }
}
