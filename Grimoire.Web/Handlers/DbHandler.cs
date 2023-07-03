using Grimoire.Sources.Interfaces;
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

    public async Task SaveSourceAsync(IGrimoireSource source) {
        await _database.CreateCollectionAsync(source.Id);
        var collection = _database.GetCollection<Manga>(source.Id);

        var mangas = await source.FetchMangasAsync();
        await collection.InsertManyAsync(mangas);
    }
}