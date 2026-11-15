using System.Net.Http.Headers;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

using Octokit;

using Shapeshifter.Website.Logic;

// Configuration: appsettings.json (no secret values) -> appsettings.{Environment}.json
// -> user-secrets (Development only) -> environment variables (e.g. GitHub__Token, Patreon__ClientSecret)
// -> command line. This is the WebApplication default order.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
	.AllowAnyHeader()
	.AllowAnyMethod()
	.AllowAnyOrigin()));

builder.Services.Configure<ForwardedHeadersOptions>(forwarded =>
{
	// The site runs behind Nginx on the same machine (loopback is a known proxy by default).
	forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
});

builder.Services.AddOptions<GitHubOptions>().BindConfiguration(GitHubOptions.SectionName);
builder.Services.AddOptions<PatreonOptions>().BindConfiguration(PatreonOptions.SectionName);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISettingsManager, SettingsManager>();

builder.Services.AddHttpClient(PatreonClient.HttpClientName, client =>
{
	client.BaseAddress = new Uri("https://www.patreon.com/");
	client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Shapeshifter.Website", "1.0"));
	client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddSingleton<IPatreonClient, PatreonClient>();

builder.Services.AddSingleton<IGitHubClient>(services =>
{
	var gitHubOptions = services.GetRequiredService<IOptions<GitHubOptions>>().Value;
	var client = new GitHubClient(new Octokit.ProductHeaderValue("Shapeshifter.Website"));
	if (!string.IsNullOrWhiteSpace(gitHubOptions.Token))
		client.Credentials = new Credentials(gitHubOptions.Token);
	return client;
});

var app = builder.Build();

// Must run first so that scheme/host (used for HTTPS redirection and the OAuth redirect URI) are correct.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
	app.UseDeveloperExceptionPage();
else
	app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

app.MapControllers();

await app.RunAsync();
