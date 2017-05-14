using System.Threading.Tasks;

namespace Shapeshifter.Website.Logic
{
  public interface IJsonWebClient
  {
    Task<T> GetAsync<T>(string url);
  }
}