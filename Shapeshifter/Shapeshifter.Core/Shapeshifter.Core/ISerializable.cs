using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Core
{
    interface ISerializable
    {
        byte[] Serialize();
    }
}
