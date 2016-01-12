namespace Shapeshifter.WindowsDesktop.Services.Clipboard.Interfaces
{
    using Data.Interfaces;

    public interface IDataSourceService
    {
        IDataSource GetDataSource();
    }
}