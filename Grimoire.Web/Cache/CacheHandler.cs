using Grimoire.Sources;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Grimoire.Web.Cache;

public record CachedManga(string Id, string CoverPath) : Manga;

public sealed class CacheHandler {
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public CacheHandler(IMemoryCache memoryCache,
                        IServiceProvider serviceProvider,
                        HttpClient httpClient,
                        IConfiguration config) {
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<IReadOnlyList<IGrimoireSource>> GetSourcesAsync() {
        var providers = _serviceProvider.GetGrimoireSources();
        var tasks = providers.Select(GetSourceAsync);
        return await Task.WhenAll(tasks);
    }

    public async Task<IGrimoireSource> GetSourceAsync(IGrimoireSource source) {
        if (_memoryCache.TryGetValue(source.Id, out IGrimoireSource sourceCache)) {
            return sourceCache;
        }

        var basePath = _config["SaveTo"].GetBasePath(source.Id);
        var iconPath = basePath.GetIconPath(source.Icon);
        if (!Directory.Exists(basePath)) {
            Directory.CreateDirectory(basePath!);
        }

        if (!File.Exists(iconPath)) {
            await _httpClient.DownloadAsync(source.Icon, basePath);
        }

        _memoryCache.Set(source.Id, sourceCache);

        return sourceCache;
    }

    public async Task<CachedManga> GetMangaAsync(IGrimoireSource source, string mangaId) {
        if (_memoryCache.TryGetValue($"{source.Id}_{mangaId}", out byte[] data)) {
            return default;
        }

        return default;
    }

    public string GetStaticPath(string str) {
        return str.Replace(_config["SaveTo"]!, "static");
    }
}