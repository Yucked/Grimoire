using Grimoire.Commons.Models;
using MongoDB.Driver;

namespace Grimoire.Web.Handlers;

public sealed class DbHandler {
    private readonly IMongoDatabase _database;
    private readonly IServiceProvider _serviceProvider;

    public DbHandler(IMongoDatabase database, IServiceProvider serviceProvider) {
        _database = database;
        _serviceProvider = serviceProvider;
    }

    public Task<List<Manga>> GetSourceAsync(string sourceId) {
        var collection = _database.GetCollection<Manga>(sourceId);
        return collection.Find(Builders<Manga>.Filter.Empty)
            .ToListAsync();
    }

    public Task<Manga> GetMangaAsync(string sourceId, string mangaId) {
        var collection = _database.GetCollection<Manga>(sourceId);
        return collection
            .Find(Builders<Manga>.Filter.Eq(x => x.Id, mangaId))
            .SingleAsync();
    }

    public async Task<Chapter> GetChapterAsync(string sourceId, string mangaId, int chapter) {
        var manga = await GetMangaAsync(sourceId, mangaId);
        return manga.Chapters[chapter];
    }

    public async Task SaveMangasAsync(string sourceId, IReadOnlyList<Manga> mangas) {
        if (!mangas.Any()) {
            return;
        }

        if (!await _database.DoesCollectionExistAsync(sourceId)) {
            await _database.CreateCollectionAsync(sourceId);
        }

        var collection = _database.GetCollection<Manga>(sourceId);
        await collection.InsertManyAsync(mangas);
    }

    public async Task UpdateMangaWithChapter(string sourceId,
                                             string mangaId,
                                             int chapterIndex,
                                             Chapter chapter) {
        var manga = await GetMangaAsync(sourceId, mangaId);
        var filter = Builders<Manga>.Filter.Eq(x => x.Id, mangaId) &
                     Builders<Manga>.Filter.Eq(x => x.Chapters, manga.Chapters);
        manga.Chapters[chapterIndex] = chapter;
        var update = Builders<Manga>.Update.Set(x => x.Chapters, manga.Chapters);

        var collection = _database.GetCollection<Manga>(sourceId);
        await collection.UpdateOneAsync(filter, update);
    }

    public Task<bool> SourceExistsAsync(string sourceId) {
        return _database.DoesCollectionExistAsync(sourceId);
    }

    public Task AddToLibraryAsync(string sourceId, string mangaId, bool shouldAdd) {
        var collection = _database.GetCollection<Manga>(sourceId);
        return collection.FindOneAndUpdateAsync(
            Builders<Manga>.Filter.Eq(x => x.Id, mangaId),
            Builders<Manga>.Update.Set(x => x.IsInLibrary, shouldAdd));
    }

    public async Task<IEnumerable<Manga>> GetLibraryAsync() {
        var collections = await (await _database.ListCollectionsAsync()).ToListAsync();
        var tasks = collections
            .Select(collection => {
                return _database.GetCollection<Manga>(collection.Elements.First().Value.AsString)
                    .Find(Builders<Manga>.Filter.Eq(x => x.IsInLibrary, true))
                    .ToListAsync();
            });

        var result = await Task.WhenAll(tasks);
        return result.SelectMany(x => x);
    }

    public async Task UpdateLibraryAsync() {
        var sources = _serviceProvider.GetGrimoireSources().ToArray();
        var library = await GetLibraryAsync();

        foreach (var manga in library) {
            var source = sources.First(x => x.Id == manga.SourceId);
            var update = await source.GetMangaAsync(manga.Url);

            var collection = _database.GetCollection<Manga>(source.Id);
            await collection.ReplaceOneAsync(Builders<Manga>.Filter.Eq(x => x.Id, manga.Id), update);
        }
    }
}