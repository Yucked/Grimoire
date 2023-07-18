using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Grimoire.Commons.Proxy;

public sealed class ProxiesHandler {
    public HashSet<string> Proxies { get; }
    public RotatingProxies RotatingProxies { get; private set; }

    private readonly ILogger<ProxiesHandler> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ProxiesHandler(ILogger<ProxiesHandler> logger, HttpClient httpClient, IConfiguration configuration) {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        Proxies = new HashSet<string>();
    }

    public async Task VerifyProxies() {
        _logger.LogInformation("Verifying proxies...");
        var proxies = default(string[]);
        foreach (var listUrl in _configuration.GetSection("Proxy:List").Get<string[]>()) {
            using var responseMessage = await _httpClient.SendAsync(new HttpRequestMessage {
                Method = HttpMethod.Get,
                RequestUri = new Uri(listUrl),
                Headers = {
                    {
                        "User-Agent", _configuration.GetSection("Proxy:UserAgents").Get<string[]>().RandomItem()
                    }
                }
            });

            if (!responseMessage.IsSuccessStatusCode) {
                _logger.LogError("Unable to fetch {}", listUrl);
                continue;
            }

            var proxyList = await responseMessage.Content.ReadAsStringAsync();
            proxies = proxyList.Split(Environment.NewLine);
        }

        await Parallel.ForEachAsync(proxies!, (x, _) => IsValidProxyAsync(x));
        RotatingProxies = new RotatingProxies(Proxies);
        _logger.LogInformation("Found {} working proxies out of {}.", Proxies.Count, proxies.Length);
    }

    private async ValueTask IsValidProxyAsync(string proxy) {
        try {
            using var requestMessage = new HttpRequestMessage {
                Method = HttpMethod.Head,
                RequestUri = new Uri($"http://{proxy}")
            };

            using var responseMessage = await _httpClient.SendAsync(requestMessage);
            if (!responseMessage.IsSuccessStatusCode) {
                _logger.LogDebug("❌ {}", proxy);
                return;
            }

            _logger.LogDebug("✅ {}", proxy);
            Proxies.Add(proxy);
        }
        catch {
            _logger.LogDebug("❌ {}", proxy);
        }
    }
}