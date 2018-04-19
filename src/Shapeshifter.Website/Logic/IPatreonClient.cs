using System.Threading.Tasks;
using Shapeshifter.Website.Models;

namespace Shapeshifter.Website
{
	public interface IPatreonClient
	{
		Task<DataWrapper<Pledge[]>> GetPledgesAsync();
	}
}