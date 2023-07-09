using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Grimoire.Web.Handlers;

public sealed class CacheHandler {
    private readonly DbHandler _dbHandler;
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IEnumerable<IGrimoireSource> _sources;
    private readonly ILogger<CacheHandler> _logger;

    public CacheHandler(DbHandler dbHandler,
                        IMemoryCache memoryCache,
                        IServiceProvider serviceProvider,
                        HttpClient httpClient,
                        IConfiguration config,
                        ILogger<CacheHandler> logger) {
        _dbHandler = dbHandler;
        _memoryCache = memoryCache;
        _sources = serviceProvider.GetGrimoireSources();
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        PopulateCache();
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

                try {
                    if (!File.Exists(path.WithIcon(source.Icon))) {
                        await _httpClient.DownloadAsync(source.Icon, path.Ave);
                    }
                }
                catch {
                    _logger.LogError("{}:{}\n{}\n{}",
                        source.Name,
                        source.BaseUrl,
                        source.Icon,
                        path);
                }

                _memoryCache.Set(source.Id, path.WithIcon(source.Icon));
                return source;
            });

        return await Task.WhenAll(tasks);
    }

    public async Task<IReadOnlyList<Manga>> GetMangasAsync(string sourceId, bool refresh) {
        IReadOnlyList<Manga> mangas;
        if (!refresh && await _dbHandler.SourceExistsAsync(sourceId)) {
            mangas = await _dbHandler.GetSourceAsync(sourceId);
        }
        else {
            mangas = await _sources
                .First(x => x.Id == sourceId)
                .FetchMangasAsync();
        }

        if (_config.GetValue<bool>("Save:MangaCover")) {
            var tasks = mangas
                .Select(async manga => {
                    if (!refresh && _memoryCache.TryGetValue($"{sourceId}@{manga.Id}", out _)) {
                        return manga;
                    }

                    var path = PathMaker
                        .New(_config["Save:To"])
                        .WithSource(sourceId)
                        .WithSource(manga.Id)
                        .Verify();

                    try {
                        if (!File.Exists(path.WithCover(manga.Cover))) {
                            await _httpClient.DownloadAsync(manga.Cover, path.Ave);
                        }
                    }
                    catch {
                        _logger.LogError("{}:{}\n{}\n{}",
                            manga.Name,
                            manga.Url,
                            manga.Cover,
                            path);
                    }

                    _memoryCache.Set($"{sourceId}@{manga.Id}", path.WithCover(manga.Cover));
                    return manga;
                });

            await Task.WhenAll(tasks);
        }

        if (_config.GetValue<bool>("Cache:Manga") &&
            !await _dbHandler.SourceExistsAsync(sourceId)) {
            await _dbHandler.SaveMangasAsync(sourceId, mangas);
        }

        return mangas;
    }

    public async Task<Manga> GetMangaAsync(string sourceId, string mangaId) {
        var manga = await _dbHandler.GetMangaAsync(sourceId, mangaId);
        if (_memoryCache.TryGetValue($"{sourceId}@{mangaId}", out _)) {
            return manga;
        }

        var path = PathMaker
            .New(_config["Save:To"])
            .WithSource(sourceId)
            .WithSource(manga.Id)
            .Verify();

        try {
            if (!File.Exists(path.WithCover(manga.Cover))) {
                await _httpClient.DownloadAsync(manga.Cover, path.Ave);
            }
        }
        catch {
            _logger.LogError("{}:{}\n{}\n{}",
                manga.Name,
                manga.Url,
                manga.Cover,
                path);
        }

        _memoryCache.Set($"{sourceId}@{manga.Id}", path.WithCover(manga.Cover));

        return manga;
    }

    public async Task<Chapter> GetChapterAsync(string sourceId, string mangaId, string chapterName) {
        var manga = await GetMangaAsync(sourceId, mangaId);
        var chapter = manga.Chapters.First(x => x.Name == chapterName);
        var index = manga.Chapters.IndexOf(chapter);

        return await GetChapterAsync(sourceId, mangaId, index);
    }

    public async Task<Chapter> GetChapterAsync(string sourceId, string mangaId, int chapterIndex) {
        if (_memoryCache.TryGetValue($"{sourceId}@{mangaId}@{chapterIndex}", out string[] pages)) {
            return new Chapter {
                Pages = pages
            };
        }

        var manga = await GetMangaAsync(sourceId, mangaId);
        if (manga.Chapters[chapterIndex].Pages?.Any() == false) {
            return manga.Chapters[chapterIndex];
        }

        var source = _sources.First(x => x.Id == sourceId);
        var chapters = await source.FetchChaptersAsync(manga);
        var chapter = await source.FetchChapterAsync(chapters[chapterIndex]);
        await _dbHandler.UpdateMangaWithChapter(sourceId, mangaId, chapterIndex, chapter);

        if (!_config.GetValue<bool>("Save:MangaChapter")) {
            return chapter;
        }

        var path = PathMaker
            .New(_config["Save:To"])
            .WithSource(sourceId)
            .WithSource(manga.Id)
            .WithChapter(chapterIndex)
            .Verify();

        var tasks = chapter.Pages
            .Select(async x => {
                try {
                    if (!File.Exists(path.WithPage(x))) {
                        await _httpClient.DownloadAsync(x, path.Ave);
                    }
                }
                catch {
                    _logger.LogError("{}:{}\n{}\n{}",
                        manga.Name,
                        manga.Url,
                        manga.Cover,
                        path);
                }

                return path.WithPage(x);
            });

        pages = await Task.WhenAll(tasks);
        _memoryCache.Set($"{sourceId}@{manga.Id}@{chapterIndex}", pages);

        return chapter;
    }

    private void PopulateCache() {
        Directory.GetDirectories(_config["Save:To"]!)
            .ToList()
            .AsParallel()
            .ForAll(sourcePath => {
                var source = sourcePath.Split('\\')[^1];
                Directory.GetDirectories(sourcePath)
                    .AsParallel()
                    .ForAll(mangaPath => {
                        var manga = mangaPath.Split('\\')[^1];
                        foreach (var file in Directory.GetFiles(mangaPath)) {
                            _memoryCache.Set($"{source}@{manga}", file);
                        }
                    });
            });
    }
}