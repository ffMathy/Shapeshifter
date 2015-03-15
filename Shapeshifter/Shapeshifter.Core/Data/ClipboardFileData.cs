using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Core.Data
{
    public class ClipboardFileData : IClipboardData
    {
        public IDataSource Source
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
