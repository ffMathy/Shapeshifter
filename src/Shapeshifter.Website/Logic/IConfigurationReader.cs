namespace Shapeshifter.Website
{
  public interface IConfigurationReader
  {
    string Read(string key);
  }
}