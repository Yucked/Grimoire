using AngleSharp;
using AngleSharp.Dom;
using Grimoire.Helpers;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Grimoire.Handlers;

public sealed class HttpHandler(
    ILogger<HttpHandler> logger,
    HttpClient httpClient,
    IConfiguration configuration) {
    private readonly IBrowsingContext _context
        = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

    private readonly SemaphoreSlim _semaphore
        = new(1, 10);

    public Task<IDocument> ParseHtmlAsync(string html) {
        return _context.OpenAsync(x => x.Content(html));
    }

    private HttpRequestMessage GetRequest(string url) {
        return new HttpRequestMessage {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
            Headers = {
                {
                    "User-Agent", configuration.GetRandomUserAgent()
                }
            }
        };
    }

    public async Task<Stream?> GetStreamAsync(string url) {
        try {
            await _semaphore.WaitAsync();
            return await await Task
                .Delay(Random.Shared.Next(configuration.GetValue<int>("Http:Delay")))
                .ContinueWith(async _ => {
                    using var responseMessage = await httpClient.SendAsync(GetRequest(url));
                    if (!responseMessage.IsSuccessStatusCode) {
                        logger.LogError("Unable to reach {}\n{}",
                            url, responseMessage.ReasonPhrase);
                        return default;
                    }

                    var bytes = await responseMessage.Content.ReadAsByteArrayAsync();
                    if (bytes.Length == 0) {
                        logger.LogError("Failed to fetch data from {}", url);
                        return default;
                    }

                    return new MemoryStream(bytes);
                });
        }
        catch (Exception exception) {
            logger.LogError("Failed to get {}\n{}", url, exception.ToDictionary());
            throw;
        }
        finally {
            _semaphore.Release();
        }
    }

    public async Task DownloadAsync(string url, string output) {
        try {
            await _semaphore.WaitAsync();
            await await Task
                .Delay(Random.Shared.Next(configuration.GetValue<int>("Http:Delay")))
                .ContinueWith(async _ => {
                    using var responseMessage = await httpClient.SendAsync(GetRequest(url));
                    if (!responseMessage.IsSuccessStatusCode) {
                        logger.LogError("Unable to reach {}\n{}",
                            url, responseMessage.ReasonPhrase);
                        return;
                    }

                    // TODO: REMOVE LATER
                    var fileName = (responseMessage.Content.Headers.ContentDisposition?.FileNameStar
                                    ?? url.Split('/')[^1]).Clean();
                    await using var fs = new FileStream($"{output}/{fileName}", FileMode.CreateNew);
                    await responseMessage.Content.CopyToAsync(fs);
                });
        }
        catch (Exception exception) {
            logger.LogError("Failed to download {}\n{}", url, exception.ToDictionary());
            throw;
        }
        finally {
            _semaphore.Release();
        }
    }

    public async Task<IDocument?> ParseAsync(string url) {
        try {
            await _semaphore.WaitAsync();
            var tries = 0;
            IDocument? document = null;

            do {
                await Task.Delay(Random.Shared.Next(configuration.GetValue<int>("Http:Delay")));
                using var responseMessage = await httpClient.SendAsync(GetRequest(url));
                if (!responseMessage.IsSuccessStatusCode) {
                    logger.LogError("Unable to reach {}\n{}",
                        url, responseMessage.ReasonPhrase);

                    tries++;
                    continue;
                }

                await using var stream = await responseMessage.Content.ReadAsStreamAsync();
                document = await _context.OpenAsync(x => x.Content(stream));
                await document.WaitForReadyAsync();
                if (document.All.Length == 3) {
                    tries++;
                    continue;
                }

                break;
            } while (tries <= configuration.GetValue<int>("Http:Retries"));

            return document;
        }
        catch (Exception exception) {
            logger.LogError("Failed to parse {}\n{}", url, exception.ToDictionary());
            throw;
        }
        finally {
            _semaphore.Release();
        }
    }
}