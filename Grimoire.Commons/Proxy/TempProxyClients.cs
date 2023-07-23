using System.Net;
using Knapcode.TorSharp;
using Microsoft.Extensions.Configuration;

namespace Grimoire.Commons.Proxy;

public class TempProxyClients {
    private readonly HttpClient _httpClient;
    private readonly TorSharpSettings _torSettings;
    private readonly TorSharpProxy _torProxy;
    private readonly HttpClient _proxyClient;

    public TempProxyClients(HttpClient httpClient, IConfiguration configuration) {
        _httpClient = httpClient;
        _torSettings = new TorSharpSettings {
            ExtractedToolsDirectory = configuration.GetValue<string>("TorDir"),
            PrivoxySettings = {
                Disable = true
            }
        };
        _torProxy = new TorSharpProxy(_torSettings);

        _proxyClient = new HttpClient(new HttpClientHandler {
            Proxy = new WebProxy(new Uri($"http://localhost:{_torSettings.PrivoxySettings.Port}"))
        });
    }

    public async Task SetupProxiesAsync() {
        var fetcher = new TorSharpToolFetcher(_torSettings, _httpClient);
        await fetcher.FetchAsync();
        await _torProxy.ConfigureAndStartAsync();

        Console.WriteLine(await _proxyClient.GetStringAsync("http://api.ipify.org"));
        await _torProxy.GetNewIdentityAsync();
        Console.WriteLine(await _proxyClient.GetStringAsync("http://api.ipify.org"));
    }
}