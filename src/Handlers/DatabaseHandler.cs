using Grimoire.Helpers;
using Grimoire.Models;
using Grimoire.Sources.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grimoire.Handlers;

public sealed class DatabaseHandler(
    IMongoDatabase database,
    IServiceProvider serviceProvider,
    IConfiguration config,
    HttpHandler httpHandler,
    IMemoryCache memoryCache,
    ILogger<DatabaseHandler> logger) {
    private readonly IEnumerable<IGrimoireSource> _sources
        = serviceProvider.GetGrimoireSources();

    public async Task InitializeAsync() {
        var collections = (await (await database.ListCollectionsAsync()).ToListAsync())
            .Select(x => database.GetCollection<Manga>(x.Elements.First().Value.AsString));
        foreach (var collection in collections) {
            await collection.Indexes.CreateManyAsync([
                new CreateIndexModel<Manga>(Builders<Manga>.IndexKeys.Text(x => x.Name)),
                new CreateIndexModel<Manga>(Builders<Manga>.IndexKeys.Wildcard(x => x.Name)),
                new CreateIndexModel<Manga>(Builders<Manga>.IndexKeys.Wildcard(x => x.Summary)),
                new CreateIndexModel<Manga>(Builders<Manga>.IndexKeys.Wildcard(x => x.Metonyms))
            ]);
        }

        Directory.GetDirectories(config["Save:To"]!)
            .ToList()
            .AsParallel()
            .ForAll(sourcePath => {
                var source = sourcePath.Split('\\')[^1];
                Directory.GetDirectories(sourcePath)
                    .AsParallel()
                    .ForAll(mangaPath => {
                        var manga = mangaPath.Split('\\')[^1];
                        foreach (var file in Directory.GetFiles(mangaPath)) {
                            memoryCache.Set($"{source}@{manga}", file);
                        }
                    });
            });
    }

    public T? Get<T>(string key) {
        return memoryCache.Get<T>(key);
    }

    public string? GetSourceIcon(string sourceId) {
        return config.GetValue<bool>("Save:SourceIcon")
            ? Get<string>(sourceId)
            : _sources.First(x => x.Id == sourceId).Icon;
    }

    public Task AddMangaAsync(Manga manga) {
        return database
            .GetCollection<Manga>(manga.SourceId)
            .InsertOneAsync(manga);
    }

    public async Task<Manga> GetMangaAsync(string sourceId, string mangaId) {
        var manga = await database
            .GetCollection<Manga>(sourceId)
            .Find(Builders<Manga>.Filter.Eq(x => x.Id, mangaId))
            .SingleAsync();

        if (memoryCache.TryGetValue($"{sourceId}@{mangaId}", out _)) {
            return manga;
        }

        var path = PathMaker
            .New(config["Save:To"]!)
            .WithSource(sourceId)
            .WithSource(manga.Id)
            .Verify();

        try {
            if (!string.IsNullOrWhiteSpace(manga.Cover) && File.Exists(path.WithCover(manga.Cover))) {
                await httpHandler.DownloadAsync(manga.Cover, path.Ave);
            }
        }
        catch {
            logger.LogError("{}:{}\n{}\n{}",
                manga.Name,
                manga.Url,
                manga.Cover,
                path);
        }

        memoryCache.Set($"{sourceId}@{manga.Id}", path.WithCover(manga.Cover));
        return manga;
    }

    public async Task<IReadOnlyCollection<Manga>> GetMangasAsync(string sourceId, bool fetchUpdates) {
        var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
        var mangas = fetchUpdates || !collections.Contains(sourceId)
            ? await _sources
                .First(x => x.Id == sourceId)
                .GetMangasAsync()
            : await database
                .GetCollection<Manga>(sourceId)
                .Find(Builders<Manga>.Filter.Empty)
                .ToListAsync();

        var tasks = mangas
            .Select(async manga => {
                if (config.GetValue<bool>("Save:MangaCover")) {
                    if (!fetchUpdates && memoryCache.TryGetValue($"{sourceId}@{manga.Id}", out _)) {
                        return;
                    }

                    var path = PathMaker
                        .New(config["Save:To"]!)
                        .WithSource(sourceId)
                        .WithSource(manga.Id)
                        .Verify();

                    try {
                        if (!File.Exists(path.WithCover(manga.Cover))) {
                            await httpHandler.DownloadAsync(manga.Cover, path.Ave);
                        }
                    }
                    catch {
                        logger.LogError("{}:{}\n{}\n{}",
                            manga.Name,
                            manga.Url,
                            manga.Cover,
                            path);
                    }

                    memoryCache.Set($"{sourceId}@{manga.Id}", path.WithCover(manga.Cover));
                }

                if (fetchUpdates || !collections.Contains(sourceId)) {
                    await database
                        .GetCollection<Manga>(manga.SourceId)
                        .ReplaceOneAsync(r => r.Id == manga.Id,
                            manga,
                            new ReplaceOptions { IsUpsert = true });
                }
            });

        await Task.WhenAll(tasks);
        return mangas;
    }

    public async Task UpdateMangaAsync(string sourceId,
                                       string mangaId,
                                       int chapterIndex,
                                       Chapter chapter) {
        var manga = await GetMangaAsync(sourceId, mangaId);
        manga.Chapters[chapterIndex] = chapter;
        await database
            .GetCollection<Manga>(sourceId)
            .UpdateOneAsync(
                Builders<Manga>.Filter.Eq(x => x.Id, mangaId) &
                Builders<Manga>.Filter.Eq(x => x.Chapters, manga.Chapters),
                Builders<Manga>.Update.Set(x => x.Chapters, manga.Chapters));
    }

    public async Task<Chapter> GetChapterAsync(string sourceId, string mangaId, int chapterIndex) {
        if (memoryCache.TryGetValue($"{sourceId}@{mangaId}@{chapterIndex}", out var pages)) {
            return new Chapter {
                Pages = pages!.As<string[]>()
            };
        }

        var manga = await GetMangaAsync(sourceId, mangaId);
        if (manga.Chapters[chapterIndex].Pages.Count != 0 == false) {
            return manga.Chapters[chapterIndex];
        }

        var source = _sources.First(x => x.Id == sourceId);
        manga = await source.GetMangaAsync(manga);
        var chapter = await source.FetchChapterAsync(manga.Chapters[chapterIndex]);
        await UpdateMangaAsync(sourceId, mangaId, chapterIndex, chapter);

        if (!config.GetValue<bool>("Save:MangaChapter")) {
            return chapter;
        }

        var path = PathMaker
            .New(config["Save:To"]!)
            .WithSource(sourceId)
            .WithSource(manga.Id)
            .WithChapter(chapterIndex)
            .Verify();

        var tasks = chapter.Pages
            .Select(async x => {
                try {
                    if (!File.Exists(path.WithPage(x))) {
                        await httpHandler.DownloadAsync(x, path.Ave);
                    }
                }
                catch {
                    logger.LogError("{}:{}\n{}\n{}",
                        manga.Name,
                        manga.Url,
                        manga.Cover,
                        path);
                }

                return path.WithPage(x);
            });

        pages = await Task.WhenAll(tasks);
        memoryCache.Set($"{sourceId}@{manga.Id}@{chapterIndex}", pages);

        return chapter;
    }

    public async Task<IReadOnlyList<IGrimoireSource>> ListSourcesAsync() {
        if (!config.GetValue<bool>("Save:SourceIcon")) {
            return _sources.ToArray();
        }

        var tasks = _sources
            .Select(async source => {
                if (memoryCache.TryGetValue(source.Id, out _)) {
                    return source;
                }

                var path = PathMaker
                    .New(config["Save:To"]!)
                    .WithSource(source.Id)
                    .Verify();

                try {
                    if (!File.Exists(path.WithIcon(source.Icon))) {
                        await httpHandler.DownloadAsync(source.Icon, path.Ave);
                    }
                }
                catch {
                    logger.LogError("{}:{}\n{}\n{}",
                        source.Name,
                        source.Url,
                        source.Icon,
                        path);
                }

                memoryCache.Set(source.Id, path.WithIcon(source.Icon));
                return source;
            });

        return await Task.WhenAll(tasks);
    }

    public async Task<bool> DoesSourceExistAsync(string sourceId) {
        var filter = new BsonDocument("name", sourceId);
        var collections = await database.ListCollectionsAsync(
            new ListCollectionsOptions {
                Filter = filter
            });
        return await collections.AnyAsync();
    }

    public Task AddToLibraryAsync(string sourceId, string mangaId, bool shouldAdd) {
        return database
            .GetCollection<Manga>(sourceId)
            .FindOneAndUpdateAsync(
                Builders<Manga>.Filter.Eq(x => x.Id, mangaId),
                Builders<Manga>.Update.Set(x => x.IsInLibrary, shouldAdd));
    }

    public async Task<IEnumerable<Manga>> GetLibraryAsync() {
        var collections = await (await database.ListCollectionsAsync()).ToListAsync();
        var tasks = collections
            .Select(collection => {
                return database.GetCollection<Manga>(collection.Elements.First().Value.AsString)
                    .Find(Builders<Manga>.Filter.Eq(x => x.IsInLibrary, true))
                    .ToListAsync();
            });

        var result = await Task.WhenAll(tasks);
        return result.SelectMany(x => x);
    }

    public async Task UpdateLibraryAsync() {
        var library = await GetLibraryAsync();

        foreach (var manga in library) {
            var source = _sources.First(x => x.Id == manga.SourceId);
            var update = await source.GetMangaAsync(manga.Url);

            var collection = database.GetCollection<Manga>(source.Id);
            await collection.ReplaceOneAsync(Builders<Manga>.Filter.Eq(x => x.Id, manga.Id), update);
        }
    }

    public async Task<IReadOnlyCollection<Manga>> SearchAsync(string search) {
        var collections = await (await database.ListCollectionsAsync()).ToListAsync();
        var filter = Builders<Manga>.Filter.Text(search, new TextSearchOptions { CaseSensitive = false }) |
                     Builders<Manga>.Filter.Regex(x => x.Name, @$"(?i)\b{search}\b") |
                     Builders<Manga>.Filter.Regex(x => x.Summary, @$"(?i)\b{search}\b") |
                     Builders<Manga>.Filter.AnyStringIn(x => x.Metonyms, search);

        return collections
            .SelectMany(x =>
                database
                    .GetCollection<Manga>(x.Elements.First().Value.AsString)
                    .Find(filter)
                    .ToList()
            )
            .ToArray();
    }
}