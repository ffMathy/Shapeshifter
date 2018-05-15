using FluffySpoon.Extensions.MicrosoftDependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shapeshifter.Website
{
	using System.Collections.Generic;

	using Logic;

	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddAssemblyTypesAsImplementedInterfaces(
				typeof(Startup).Assembly);

			services.AddMvc();

			services.AddTransient<IConfigurationReader>(provider => new ConfigurationReader("secrets.json"));
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseCors(builder => builder
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowAnyOrigin());

			app.UseDefaultFiles();
			app.UseStaticFiles();

			app.UseMvc(
				routes => {
					routes.MapRoute(
						name: "default",
						template: "{controller=Home}/{action=Index}/{id?}");
				});
		}
	}
}
