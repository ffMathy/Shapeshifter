using System.Collections.Generic;
using System.Threading.Tasks;
using Shapeshifter.Website.Models;

namespace Shapeshifter.Website
{
	public interface IPatreonClient
	{
		Task<Pledge[]> GetPledgesAsync();
		Task<User> GetUserById(string id);
	}
}