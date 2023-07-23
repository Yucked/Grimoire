using AngleSharp;
using AngleSharp.Dom;
using Grimoire.Commons.Proxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Grimoire.Commons;

public class HtmlParser {
    private readonly ILogger<HtmlParser> _logger;
    private readonly ProxiesHandler _proxiesHandler;
    private readonly IBrowsingContext _context;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _proxyClient, _httpClient;

    public HtmlParser(ILogger<HtmlParser> logger,
                      ProxiesHandler proxiesHandler,
                      HttpClient httpClient,
                      IConfiguration configuration) {
        _logger = logger;
        _proxiesHandler = proxiesHandler;
        _context = BrowsingContext.New(
            Configuration.Default.WithDefaultLoader()
        );
        _proxyClient = new HttpClient(new HttpClientHandler {
            UseCookies = false,
            Proxy = proxiesHandler.RotatingProxies
        });
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<IDocument> ParseAsync(string url, bool useProxy = false) {
        var retries = 0;
        IDocument document;
        do {
            var content = await GetContentAsync(url, useProxy);
            await using var stream = await content.ReadAsStreamAsync();
            document = await _context.OpenAsync(x => x.Content(stream));
            await document.WaitForReadyAsync();
            if (document.All.Length == 3) {
                retries++;
                continue;
            }

            break;
        } while (retries <= _configuration.GetValue<int>("Proxy:MaxRetries"));

        return document;
    }

    public async Task<HttpContent> GetContentAsync(string url, bool useProxy) {
        try {
            using var requestMessage = new HttpRequestMessage {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
                Headers = {
                    {
                        "User-Agent", _configuration.GetSection("Proxy:UserAgents").Get<string[]>().RandomItem()
                    }
                }
            };

            await Task.Delay(Random.Shared.Next(_configuration.GetValue<int>("Proxy:MaxDelay")));
            using var responseMessage = useProxy
                ? await _proxyClient.SendAsync(requestMessage)
                : await _httpClient.SendAsync(requestMessage);
            if (responseMessage.IsSuccessStatusCode) {
                return responseMessage.Content;
            }

            _logger.LogError("{}\n{}", responseMessage.StatusCode, responseMessage.ReasonPhrase);
            throw new Exception(responseMessage.ReasonPhrase);
        }
        catch (Exception exception) {
            _logger.LogError("Failed to get {}\n{}", url, exception);
            if (exception is not HttpRequestException) {
                throw;
            }

            _logger.LogError("Marked proxy {} as failed", _proxiesHandler.RotatingProxies.Active.Host);
            _proxiesHandler.RotatingProxies.MarkCurrentFailed();
            throw;
        }
    }

    public Task<IDocument> ParseHtmlAsync(string html) {
        return _context.OpenAsync(x => x.Content(html));
    }

    public async Task DownloadAsync(string url, string output) {
        try {
            var content = await GetContentAsync(url, true);
            var fileName =
                (content.Headers.ContentDisposition?.FileNameStar
                 ?? url.Split('/')[^1]).Clean();
            await using var fs = new FileStream($"{output}/{fileName}", FileMode.CreateNew);
            await content.CopyToAsync(fs);
        }
        catch {
            _logger.LogError("Failed to download {}", url);
        }
    }
}