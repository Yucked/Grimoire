using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Grimoire.Web.Handlers.Models;
using MongoDB.Driver;

namespace Grimoire.Web.Handlers;

public sealed class LibraryHandler {
    private readonly IMongoDatabase _database;
    private readonly DbHandler _dbHandler;
    private readonly ILogger<LibraryHandler> _logger;

    public LibraryHandler(IMongoDatabase database, DbHandler dbHandler, ILogger<LibraryHandler> logger) {
        _database = database;
        _dbHandler = dbHandler;
        _logger = logger;
    }

    private async Task<UserLibrary> GetUserLibraryAsync() {
        if (!await _database.DoesCollectionExistAsync("Library")) {
            await _database.CreateCollectionAsync("Library");
            await _database.GetCollection<UserLibrary>("Library")
                .InsertOneAsync(new UserLibrary {
                    Favourites = new List<string>()
                });
        }

        return await _database
            .GetCollection<UserLibrary>("Library")
            .Find(Builders<UserLibrary>.Filter.Eq(x => x.Id, nameof(Grimoire)))
            .SingleAsync();
    }

    public async Task<IReadOnlyCollection<Manga>> GetLibraryAsync() {
        var library = await GetUserLibraryAsync();
        var tasks = library
            .Favourites
            .Select(x => {
                var split = x.Split('@');
                return _dbHandler
                    .GetMangaAsync(split[0], split[1]);
            });

        return await Task.WhenAll(tasks);
    }

    public async Task<IEnumerable<Manga>> GetRecentUpdatesAsync() {
        var library = await GetUserLibraryAsync();
        var tasks = library
            .Favourites
            .Select(x => {
                var split = x.Split('@');
                return _dbHandler
                    .GetMangaAsync(split[0], split[1]);
            });

        var mangas = await Task.WhenAll(tasks);
        return mangas
            .OrderBy(x => x.Chapters.OrderBy(y => y.ReleasedOn))
            .Take(10);
    }

    public async Task AddToLibraryAsync(string sourceId, string mangaId) {
        var library = await GetUserLibraryAsync();
        var filter = Builders<UserLibrary>.Filter
            .Eq(x => x.Id, nameof(Grimoire));
        var update = Builders<UserLibrary>.Update
            .Set(x => x.Favourites, library.Favourites);

        library.Favourites.Add($"{sourceId}@{mangaId}");
        await _database.GetCollection<UserLibrary>("Library")
            .UpdateOneAsync(filter, update);
        _logger.LogInformation("Added {} to library.",
            mangaId.GetNameFromId());
    }
}