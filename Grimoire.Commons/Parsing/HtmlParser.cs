using AngleSharp;
using AngleSharp.Dom;
using Grimoire.Commons.Proxy;
using Microsoft.Extensions.Logging;

namespace Grimoire.Commons.Parsing;

public class HtmlParser {
    private readonly ILogger<HtmlParser> _logger;
    private readonly ParserOptions _parserOptions;
    private readonly IBrowsingContext _context;
    private HttpClient _httpClient;

    public HtmlParser(ILogger<HtmlParser> logger, ProxiesHandler proxiesHandler, ParserOptions parserOptions) {
        _logger = logger;
        _parserOptions = parserOptions;
        _context = BrowsingContext.New(
            Configuration.Default.WithDefaultLoader()
        );

        Task.Run(async () => {
            var proxies = await proxiesHandler.VerifyProxies(parserOptions);
            _httpClient = new HttpClient(new HttpClientHandler {
                UseCookies = false,
                Proxy = new RotatingProxy(proxies)
            });
        });
    }

    public async Task<IDocument> ParseAsync(string url) {
        var retries = 0;
        do {
            try {
                using var requestMessage = new HttpRequestMessage {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url),
                    Headers = {
                        {
                            "User-Agent", _parserOptions.UserAgents.RandomItem()
                        }
                    }
                };

                await Task.Delay(Random.Shared.Next(_parserOptions.MaxDelay));
                using var responseMessage = await _httpClient.SendAsync(requestMessage);
                if (!responseMessage.IsSuccessStatusCode) {
                    _logger.LogError("{}\n{}", responseMessage.StatusCode, responseMessage.ReasonPhrase);
                    throw new Exception(responseMessage.ReasonPhrase);
                }

                using var content = responseMessage.Content;
                await using var stream = await content.ReadAsStreamAsync();
                var document = await _context.OpenAsync(x => x.Content(stream));
                await document.WaitForReadyAsync();

                if (document.All.Length != 3) {
                    return document;
                }

                retries++;
            }
            catch (Exception exception) {
                _logger.LogError("{}", exception);
            }
        } while (retries <= _parserOptions.MaxRetries);

        _logger.LogError("Failed to parse {}", url);
        throw new Exception($"Failed to parse {url}");
    }
}