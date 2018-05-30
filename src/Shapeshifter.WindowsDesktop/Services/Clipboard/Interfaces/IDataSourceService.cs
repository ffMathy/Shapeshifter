namespace Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces
{
	using System.Threading.Tasks;

	using Data.Interfaces;

    public interface IDataSourceService
    {
        Task<IDataSource> GetDataSourceAsync();
    }
}