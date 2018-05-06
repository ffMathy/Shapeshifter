namespace Shapeshifter.Website.Logic
{
	using System.Threading.Tasks;

	using Models.Patreon.Response;

	public interface IPatreonClient
	{
		Task<DataWrapper<Pledge[]>> GetPledgesAsync();
	}
}