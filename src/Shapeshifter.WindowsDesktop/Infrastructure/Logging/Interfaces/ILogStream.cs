using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging.Interfaces
{
    public interface ILogStream
    {
        void WriteLine(string input);
    }
}
