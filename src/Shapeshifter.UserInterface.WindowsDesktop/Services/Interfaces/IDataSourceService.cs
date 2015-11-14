#region

using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces
{
    public interface IDataSourceService
    {
        IDataSource GetDataSource();
    }
}