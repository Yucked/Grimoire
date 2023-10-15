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

    public async Task<IDocument> ParseAsync(string url) {
        var retries = 0;
        IDocument document;
        do {
            var content = await GetContentAsync(url);
            await using var stream = await content.ReadAsStreamAsync();
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

    public async Task<HttpContent> GetContentAsync(string url) {
        try {
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
    }

    public Task<IDocument> ParseHtmlAsync(string html) {
        return _context.OpenAsync(x => x.Content(html));
    }

    public async Task DownloadAsync(string url, string output) {
        try {
            var content = await GetContentAsync(url);
            var fileName =
                (content.Headers.ContentDisposition?.FileNameStar
                 ?? url.Split('/')[^1]).Clean();
            await using var fs = new FileStream($"{output}/{fileName}", FileMode.CreateNew);
            await content.CopyToAsync(fs);
        }
        catch {
            logger.LogError("Failed to download {}", url);
        }
    }
}