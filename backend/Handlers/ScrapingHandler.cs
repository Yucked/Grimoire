using Microsoft.Playwright;

namespace Grimoire.Handlers;

public sealed class ScrapingHandler(
    ILogger<ScrapingHandler> logger,
    IBrowser browser,
    HttpClient httpClient,
    DatabaseHandler databaseHandler) {
    private static readonly string[] BlockedResources = [
        "adzerk",
        "analytics",
        "cdn.api.twitter",
        "doubleclick",
        "exelator",
        "facebook",
        "fontawesome",
        "google",
        "google-analytics",
        "googletagmanager",
        "googlesyndication",
        "disqus",
        "ads"
    ];

    private readonly SemaphoreSlim _semaphore
        = new(1, 10);

    public async Task<IPage> RequestPageAsync(string url) {
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.RouteAsync("**/*", async route => {
            if (BlockedResources.Any(y => route.Request.Url.Contains(y))) {
                logger.LogWarning("Route matched with blocked resources, aborted: {}", route.Request.Url);
                await route.AbortAsync();
                return;
            }

            await route.ContinueAsync();
        });

        var response = await page.GotoAsync(url, new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        if (response?.Ok is not true) {
            logger.LogError("Failed to fetch {}", url);
            return null;
            // Fallback on AngleSharp
        }

        await response?.FinishedAsync()!;
        return page;
    }

    public async Task DownloadAsync(string sourceId, string mangaId, string url) {
        if (string.IsNullOrWhiteSpace(url)) {
            logger.LogError("Unable to download {} from {} for {}", url, sourceId, mangaId);
            return;
        }

        try {
            await _semaphore.WaitAsync();
            await await Task
                .Delay(Random.Shared.Next(2000, 4000))
                .ContinueWith(async _ => {
                    using var responseMessage = await httpClient.GetAsync(url);
                    if (!responseMessage.IsSuccessStatusCode) {
                        logger.LogError("Unable to reach {}\n{}",
                            url, responseMessage.ReasonPhrase);
                        return;
                    }

                    var fileName = responseMessage.Content.Headers.ContentDisposition?.FileNameStar
                                   ?? url.Split('/')[^1];
                    var ms = new MemoryStream();
                    await responseMessage.Content.CopyToAsync(ms);
                    databaseHandler.StoreImage(sourceId, mangaId, fileName, ms);
                });
        }
        catch (Exception exception) {
            logger.LogError("Failed to download {}\n{}", url, exception.AsKV());
            throw;
        }
        finally {
            _semaphore.Release();
        }
    }
}