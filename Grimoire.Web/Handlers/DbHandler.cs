using Grimoire.Sources.Models;
using MongoDB.Driver;

namespace Grimoire.Web.Handlers;

public sealed class DbHandler {
    private readonly IMongoDatabase _database;

    public DbHandler(IMongoDatabase database) {
        _database = database;
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

    public async Task SaveMangasAsync(string sourceId, IEnumerable<Manga> mangas) {
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

    public Task AddToLibraryAsync(string sourceId, string mangaId) {
        return _database.GetCollection<Manga>(sourceId)
            .FindOneAndUpdateAsync(
                Builders<Manga>.Filter.Eq(x => x.Id, mangaId),
                Builders<Manga>.Update.Set(x => x.IsInLibrary, true));
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
}