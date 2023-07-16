using System.Net;
using Grimoire.Commons.Parsing;
using Microsoft.Extensions.Logging;

namespace Grimoire.Commons.Proxy;

public sealed class ProxiesHandler {
    private readonly ILogger<ProxiesHandler> _logger;
    private readonly HttpClient _httpClient;

    public ProxiesHandler(ILogger<ProxiesHandler> logger, HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string[]> VerifyProxies(ParserOptions options) {
        var tasks = options.Proxies
            .Select(x => IsValidProxyAsync(x, options.UserAgents.RandomItem()));

        var results = await Task.WhenAll(tasks);
        return results
            .Where(x => x.IsValid)
            .Select(x => x.Proxy)
            .ToArray();
    }

    private async Task<(string Proxy, bool IsValid)> IsValidProxyAsync(string proxy, string userAgent) {
        try {
            using var requestMessage = new HttpRequestMessage {
                Method = HttpMethod.Head,
                RequestUri = new Uri($"http://{proxy}"),
                Headers = {
                    {
                        "User-Agent", userAgent
                    }
                }
            };

            using var responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode) {
                _logger.LogTrace("Proxy Bad {}", proxy);
                return (proxy, false);
            }

            _logger.LogTrace("Proxy OK {}", proxy);
            return (proxy, responseMessage.StatusCode == HttpStatusCode.OK);
        }
        catch {
            _logger.LogTrace("Proxy Bad {}", proxy);
            return (proxy, false);
        }
    }
}