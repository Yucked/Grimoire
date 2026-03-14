using Grimoire.Handlers;
using Grimoire.Services;
using Grimoire.Sources.MetadataProviders;
using Microsoft.Playwright;
using Minio;
using Raven.Client.Documents;

namespace Grimoire;

public sealed class Program {
    private static async Task Main(string[] args) {
        var browserPath = Path.Combine(Directory.GetCurrentDirectory(), "playwright");
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserPath);

        if (!Directory.Exists(Path.Combine(browserPath, "chromium-*"))) {
            var exitCode = Microsoft.Playwright.Program.Main(
            [
                "install",
                "--with-deps",
                "chromium"
            ]);

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
        builder.Services
            .AddHostedService<DatabaseBackgroundService>()
            .AddHostedService<ChapterDownloadService>()
            .AddHostedService<LibraryBackgroundService>()
            .AddSingleton<ServiceCoordinator>()
            .AddSingleton<DatabaseHandler>()
            .AddSingleton<ScrapingHandler>()
            .AddSingleton<MangaDexProvider>()
            .AddTransient<MyAnimeListProvider>()
            .AddSingleton(browser)
            .AddMinio(x => {
                x.WithEndpoint(config.GetValue<string>("Minio:Endpoint"));
                x.WithCredentials(
                    config.GetValue<string>("Minio:AccessKey"),
                    config.GetValue<string>("Minio:SecretKey"));
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
            .AddFlareHttpClient(builder.Configuration)
            .AddSingleton<ScrapingHandler>()
            .AddSingleton<MangaDexProvider>()
            .AddSingleton<MyAnimeListProvider>();

        var app = builder.Build();
        app.MapControllers();
        await app.RunAsync();
    }
}