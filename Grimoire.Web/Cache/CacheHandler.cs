using Grimoire.Providers;
using Grimoire.Providers.Interfaces;
using Grimoire.Providers.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Grimoire.Web.Cache;

public readonly record struct CachedProvider(string Id, string Name, string IconPath, IReadOnlyList<Manga> Mangas);

public record CachedManga(string Id, string CoverPath) : Manga;

public sealed class CacheHandler {
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    private readonly CacheOptions _options;

    public CacheHandler(IMemoryCache memoryCache,
                        IServiceProvider serviceProvider,
                        HttpClient httpClient,
                        CacheOptions options) {
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<CachedProvider>> GetProvidersAsync() {
        var providers = _serviceProvider.GetGrimoireProviders();
        var tasks = providers.Select(GetProviderAsync);
        return await Task.WhenAll(tasks);
    }

    public async Task<CachedProvider> GetProviderAsync(IGrimoireProvider prov) {
        if (_memoryCache.TryGetValue(prov.Name.GetId(), out CachedProvider cachedProvider)) {
            return cachedProvider;
        }

        var basePath = _options.SaveTo.GetBasePath(prov.Name.GetId());
        var iconPath = basePath.GetIconPath(prov.Icon);
        if (!Directory.Exists(basePath)) {
            Directory.CreateDirectory(basePath!);
        }

        if (!File.Exists(iconPath)) {
            await _httpClient.DownloadAsync(prov.Icon, basePath);
        }

        var mangas = await prov.FetchMangasAsync();
        var tasks = mangas.Select(async x => {
            var mPath = Path.Combine(basePath, x.Name.GetId());
            var coverPath = mPath.GetIconPath(x.Cover);

            if (!Directory.Exists(mPath)) {
                Directory.CreateDirectory(mPath!);
            }

            if (!File.Exists(coverPath)) {
                await _httpClient.DownloadAsync(x.Cover, $"{prov.Name.GetId()}@{x.Name.GetId()}");
            }

            return new CachedManga($"{prov.Name.GetId()}@{x.Name.GetId()}", coverPath);
        });

        cachedProvider = new CachedProvider {
            Id = prov.Name.GetId(),
            Name = prov.Name,
            IconPath = iconPath,
            Mangas = await Task.WhenAll(tasks)
        };

        _memoryCache.Set(prov.Name.GetId(), cachedProvider);

        return cachedProvider;
    }

    public async Task<CachedManga> GetMangaAsync(CachedProvider prov, string name) {
        if (_memoryCache.TryGetValue($"{prov.Name.GetId()}@{name.GetId()}", out byte[] data)) {
            return data.Decode<CachedManga>();
        }

        return default;
    }

    public string GetStaticPath(string str) {
        return str.Replace(_options.SaveTo, "static");
    }
}