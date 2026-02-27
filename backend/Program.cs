using FlareSolverrSharp;
using Grimoire;
using Grimoire.Handlers;
using Grimoire.Integrations;
using Grimoire.Objects;
using Grimoire.Services;
using Grimoire.Sources;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Playwright;
using Minio;
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

        var config = builder.Configuration;

        builder.Services.AddControllers();
        builder.Services.AddGrimoireSources();
        builder.Services
            .AddOutputCache()
            .AddHostedService<DatabaseBackgroundService>()
            .AddHostedService<LibraryService>()
            .AddHostedService<ChapterDownloadService>()
            .AddSingleton<ServiceCoordinator>()
            .AddSingleton<DatabaseHandler>()
            .AddSingleton<ScrapingHandler>()
            .AddSingleton<DownloadQueue>()
            .AddTransient<IMetadataProvider, MangaDexProvider>()
            .AddTransient<IMetadataProvider, MyAnimeListProvider>()
            .AddSingleton(browser)
            .AddMinio(x => {
                var ep = config.GetValue<string>("Minio:Endpoint");
                x.WithEndpoint(ep);
                x.WithCredentials(config.GetValue<string>("Minio:AccessKey"), config.GetValue<string>("Minio:SecretKey"));
            })
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

        builder.Services.AddHttpClient<MangaDexProvider>(c =>
            c.BaseAddress = new Uri("https://api.mangadex.org"));

        var app = builder.Build();

        app.UseExceptionHandler(errorApp => {
            errorApp.Run(async context => {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                var error = context.Features.Get<IExceptionHandlerFeature>();
                app.Logger.LogError(error?.Error, "Unhandled exception");
                await context.Response.WriteAsJsonAsync(
                    ResponseObject.New(StatusCodes.Status500InternalServerError, "An unexpected error occurred."));
            });
        });

        app.MapControllers();
        app.UseOutputCache();

        await app.RunAsync();
    }
}
