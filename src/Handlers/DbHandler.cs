using Grimoire.Helpers;
using Grimoire.Models;
using MongoDB.Driver;

namespace Grimoire.Handlers;

public sealed class DbHandler(IMongoDatabase database, IServiceProvider serviceProvider) {
    public Task<List<Manga>> GetSourceAsync(string sourceId) {
        var collection = database.GetCollection<Manga>(sourceId);
        return collection.Find(Builders<Manga>.Filter.Empty)
            .ToListAsync();
    }

    public Task<Manga> GetMangaAsync(string sourceId, string mangaId) {
        var collection = database.GetCollection<Manga>(sourceId);
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

        if (!await database.DoesCollectionExistAsync(sourceId)) {
            await database.CreateCollectionAsync(sourceId);
        }

        var collection = database.GetCollection<Manga>(sourceId);
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

        var collection = database.GetCollection<Manga>(sourceId);
        await collection.UpdateOneAsync(filter, update);
    }

    public Task<bool> SourceExistsAsync(string sourceId) {
        return database.DoesCollectionExistAsync(sourceId);
    }

    public Task AddToLibraryAsync(string sourceId, string mangaId, bool shouldAdd) {
        var collection = database.GetCollection<Manga>(sourceId);
        return collection.FindOneAndUpdateAsync(
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
        var sources = serviceProvider.GetGrimoireSources().ToArray();
        var library = await GetLibraryAsync();

        foreach (var manga in library) {
            var source = sources.First(x => x.Id == manga.SourceId);
            var update = await source.GetMangaAsync(manga.Url);

            var collection = database.GetCollection<Manga>(source.Id);
            await collection.ReplaceOneAsync(Builders<Manga>.Filter.Eq(x => x.Id, manga.Id), update);
        }
    }

    public async Task SearchAsync(string search) {
        var collections = await (await database.ListCollectionsAsync()).ToListAsync();
        var filter = Builders<Manga>.Filter.Regex(x => x.Name, search) |
                     Builders<Manga>.Filter.AnyStringIn(x => x.Metonyms, search);

        var tasks = collections
            .Select(x =>
                database
                    .GetCollection<Manga>(x.Elements.First().Value.AsString)
                    .Find(filter)
                    .ToListAsync()
            );

        var result = await Task.WhenAll(tasks);
        var asd = result;
    }
}