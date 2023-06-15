using Grimoire.Db.Models;
using Grimoire.Providers;
using StackExchange.Redis;

namespace Grimoire.Db;

public partial class Cache {
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IServiceProvider _provider;
    private readonly HttpClient _httpClient;
    private readonly CacheOptions _options;

    public Cache(IConnectionMultiplexer multiplexer, IServiceProvider provider, HttpClient httpClient,
                 CacheOptions options) {
        _multiplexer = multiplexer;
        _provider = provider;
        _httpClient = httpClient;
        _options = options;
    }

    public async Task StoreProvidersAsync() {
        foreach (var provider in _provider.GetGrimoireProviders()) {
            var db = _multiplexer.GetDatabase(0);
            if (!Directory.Exists($"{_options.SaveTo}/{provider.GetId()}")) {
                Directory.CreateDirectory($"{_options.SaveTo}/{provider.GetId()}");
            }

            var icon = await _httpClient.DownloadAsync(provider.Icon, $"{_options.SaveTo}/{provider.GetId()}");
            var mangas = await provider.FetchMangasAsync();

            var cache = new CacheProvider {
                Id = provider.GetId(),
                Mangas = mangas,
                IconPath = icon
            };
            await db.StringSetAsync(provider.GetId(), cache.Encode());
        }
    }

    public async Task StoreChapterAsync() { }
}