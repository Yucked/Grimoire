using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Grimoire.Commons;

public class HtmlParser(ILogger<HtmlParser> logger,
                        HttpClient httpClient,
                        IConfiguration configuration) {
    private readonly IBrowsingContext _context = BrowsingContext.New(
        Configuration.Default.WithDefaultLoader()
    );

    private readonly SemaphoreSlim _semaphore = new(1, 10);

    public async Task<HttpContent> GetContentAsync(string url) {
        try {
            await _semaphore.WaitAsync();
            var requestMessage = new HttpRequestMessage {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
                Headers = {
                    {
                        "User-Agent", configuration.GetSection("Http:UserAgents").Get<string[]>().RandomItem()
                    }
                }
            };

            await Task.Delay(Random.Shared.Next(configuration.GetValue<int>("Http:Delay")));
            using var responseMessage = await httpClient.SendAsync(requestMessage);
            if (responseMessage.IsSuccessStatusCode) {
                return responseMessage.Content;
            }

            logger.LogError("{}\n{}", responseMessage.StatusCode, responseMessage.ReasonPhrase);
            throw new Exception(responseMessage.ReasonPhrase);
        }
        catch (Exception exception) {
            logger.LogError("Failed to get {}\n{}", url, exception);
            if (exception is not HttpRequestException) {
                throw;
            }

            throw;
        }
        finally {
            _semaphore.Release();
        }
    }

    public Task<IDocument> ParseHtmlAsync(string html) {
        return _context.OpenAsync(x => x.Content(html));
    }

    public async Task DownloadAsync(string url, string output) {
        try {
            await _semaphore.WaitAsync();
            var requestMessage = new HttpRequestMessage {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
                Headers = {
                    {
                        "User-Agent", configuration.GetSection("Http:UserAgents").Get<string[]>().RandomItem()
                    }
                }
            };

            await Task.Delay(Random.Shared.Next(configuration.GetValue<int>("Http:Delay")));
            using var responseMessage = await httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode) {
                logger.LogError("{}\n{}", responseMessage.StatusCode, responseMessage.ReasonPhrase);
                throw new Exception(responseMessage.ReasonPhrase);
            }

            var fileName = (responseMessage.Content.Headers.ContentDisposition?.FileNameStar
                            ?? url.Split('/')[^1]).Clean();
            await using var fs = new FileStream($"{output}/{fileName}", FileMode.CreateNew);
            await responseMessage.Content.CopyToAsync(fs);
        }
        catch (Exception exception) {
            logger.LogError("Failed to download {}\n{}", url, exception);
        }
        finally {
            _semaphore.Release();
        }
    }

    public async Task<IDocument> ParseAsync(string url) {
        try {
            var retries = 0;
            IDocument document;
            do {
                await _semaphore.WaitAsync();
                var requestMessage = new HttpRequestMessage {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url),
                    Headers = {
                        {
                            "User-Agent", configuration.GetSection("Http:UserAgents").Get<string[]>().RandomItem()
                        }
                    }
                };

                await Task.Delay(Random.Shared.Next(configuration.GetValue<int>("Http:Delay")));
                using var responseMessage = await httpClient.SendAsync(requestMessage);
                if (!responseMessage.IsSuccessStatusCode) {
                    logger.LogError("{}\n{}", responseMessage.StatusCode, responseMessage.ReasonPhrase);
                    throw new Exception(responseMessage.ReasonPhrase);
                }

                await using var stream = await responseMessage.Content.ReadAsStreamAsync();
                document = await _context.OpenAsync(x => x.Content(stream));
                await document.WaitForReadyAsync();
                if (document.All.Length == 3) {
                    retries++;
                    continue;
                }

                break;
            } while (retries <= configuration.GetValue<int>("Http:Retries"));

            return document;
        }
        catch (Exception exception) {
            logger.LogError("Failed to get {}\n{}", url, exception);
            throw;
        }
        finally {
            _semaphore.Release();
        }
    }
}