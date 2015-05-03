using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;

namespace Shapeshifter.Core.Factories.Interfaces
{
    public interface IDataSourceFactory
    {
        IDataSource GetDataSource();
    }
}
