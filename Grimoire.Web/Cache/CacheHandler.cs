using Grimoire.Sources;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Grimoire.Web.Cache;

public sealed class CacheHandler {
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IEnumerable<IGrimoireSource> _sources;

    public CacheHandler(IMemoryCache memoryCache,
                        IServiceProvider serviceProvider,
                        HttpClient httpClient,
                        IConfiguration config) {
        _memoryCache = memoryCache;
        _sources = serviceProvider.GetGrimoireSources();
        _httpClient = httpClient;
        _config = config;
    }

    public T Get<T>(string key) {
        return _memoryCache.Get<T>(key);
    }

    public async Task<IReadOnlyList<IGrimoireSource>> GetSourcesAsync() {
        if (!_config.GetValue<bool>("Save:SourceIcon")) {
            return _sources.ToArray();
        }

        var tasks = _sources
            .Select(async source => {
                if (_memoryCache.TryGetValue(source.Id, out _)) {
                    return source;
                }

                var path = PathMaker
                    .New(_config["Save:To"])
                    .WithSource(source.Id)
                    .Verify();

                if (!File.Exists(path.WithIcon(source.Icon))) {
                    await _httpClient.DownloadAsync(source.Icon, path.Ave);
                }

                _memoryCache.Set(source.Id, path.WithIcon(source.Icon));

                return source;
            });

        return await Task.WhenAll(tasks);
    }

    public async Task<IReadOnlyList<Manga>> GetMangasAsync(string sourceId) {
        var source = _sources.First(x => x.Id == sourceId);
        if (_memoryCache.TryGetValue($"{sourceId}@Mangas", out IReadOnlyList<Manga> mangas)) {
            return mangas;
        }

        mangas = await source.FetchMangasAsync();
        if (_config.GetValue<bool>("Save:MangaCover")) {
            var tasks = mangas
                .Select(async manga => {
                    if (_memoryCache.TryGetValue($"{sourceId}@{manga.Id}", out _)) {
                        return manga;
                    }

                    var path = PathMaker
                        .New(_config["Save:To"])
                        .WithSource(sourceId)
                        .WithSource(manga.Id)
                        .Verify();

                    if (!File.Exists(path.WithCover(manga.Cover))) {
                        await _httpClient.DownloadAsync(manga.Cover, path.Ave);
                    }

                    _memoryCache.Set($"{sourceId}@{manga.Id}", path.WithCover(manga.Cover));
                    return manga;
                });

            await Task.WhenAll(tasks);
        }

        if (!_config.GetValue<bool>("Cache:Manga")) {
            return mangas;
        }

        _memoryCache.Set($"{sourceId}@Mangas", mangas);
        return mangas;
    }

    public async Task<Manga> GetMangaAsync(string sourceId, string mangaId) {
        if (!_memoryCache.TryGetValue($"{sourceId}@Mangas", out IReadOnlyCollection<Manga> mangas)) {
            //TODO: Fetch individual manga?
            return default;
        }

        return mangas.First(x => x.Id == mangaId);
    }

    public async Task<Chapter> GetChapterAsync(string sourceId, string mangaId, int chapterIndex) {
        if (!_memoryCache.TryGetValue($"{sourceId}@Mangas", out IReadOnlyList<Manga> mangas)) {
            //TODO: Fetch individual manga?
            return default;
        }

        var manga = mangas.FirstOrDefault(x => x.Id == mangaId);
        if (manga.Chapters?[chapterIndex].Pages.IsNullOrEmpty() == false) {
            return manga.Chapters[chapterIndex];
        }

        var source = _sources.First(x => x.Id == sourceId);
        var chapters = await source.FetchChaptersAsync(manga);

        var chapter = source
        if (!_config.GetValue<bool>("Cache:MangaChapter")) {
            return chapters[chapterIndex];
        }

        mangas.Where(x => x.Id == mangaId).ToList().ForEach(x => x.Chapters = chapters);
        _memoryCache.Set($"{sourceId}@Mangas", mangas);

        return manga.Chapters[chapterIndex];
    }
}