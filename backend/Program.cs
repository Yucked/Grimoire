using FlareSolverrSharp;
using Grimoire;
using Grimoire.Handlers;
using Grimoire.Services;
using Grimoire.Sources;
using Microsoft.Playwright;
using Raven.Client.Documents;

public sealed class Program {
    private static async Task Main(string[] args) {
        var browserPath = Path.Combine(Directory.GetCurrentDirectory(), "playwright");
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserPath);

        if (!Directory.Exists(Path.Combine(browserPath, "chromium-*"))) {
            var exitCode = Microsoft.Playwright.Program.Main(
                ["install",
                "--with-deps",
                "chromium"]);

            if (exitCode != 0) {
                throw new Exception($"Playwright exited with code {exitCode}");
            }
        }

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();

        var builder = WebApplication.CreateSlimBuilder(args);
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile("config.json", false, true);

        builder.Services.AddControllers();
        builder.Services
            .AddOutputCache()
            .AddHostedService<DatabaseBackgroundService>()
            .AddHostedService<MangaBackgroundService>()
            .AddSingleton<ServiceCoodrinator>()
            .AddSingleton<DatabaseHandler>()
            .AddSingleton<ScrapingHandler>()
            .AddSingleton<TCBScansSource>()
            .AddSingleton(browser)
            .AddSingleton(x => new DocumentStore {
                Urls = builder.Configuration.GetSection("RavenNodes").Get<string[]>(),
                Conventions = {
            CreateHttpClient = _ => x.GetService<IHttpClientFactory>()!.CreateClient("RavenDB"),
            UseOptimisticConcurrency = true,
            MaxNumberOfRequestsPerSession = 30,
            RequestTimeout = TimeSpan.FromSeconds(15)
                },
                Database = nameof(Grimoire)
            }.Initialize())
            .AddHttpClient<ScrapingHandler>()
            .ConfigurePrimaryHttpMessageHandler(() =>
            new ClearanceHandler(builder.Configuration.GetValue<string>("Http:FlareUrl")) {
                MaxTimeout = builder.Configuration.GetValue<int>("Http:FlareTimeout")
            });

        var app = builder.Build();
        app.MapControllers();
        app.UseOutputCache(); // what does it do?
                              // app.UseResponseCaching();
                              // app.UseResponseCompression();

        await app.RunAsync();
    }
}