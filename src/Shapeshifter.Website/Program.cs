using System.IO;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Shapeshifter.Website
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseKestrel(opt =>
				{
					opt.Listen(IPAddress.Loopback, 5002);
				});
	}
}
