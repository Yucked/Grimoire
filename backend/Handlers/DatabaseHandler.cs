using Grimoire.Objects;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace Grimoire.Handlers;

public sealed partial class DatabaseHandler(IDocumentStore documentStore) {
    public async Task StoreAsync<T>(T item) {
        using var session = documentStore.OpenAsyncSession();
        var task = item switch {
            MangaObject manga   => session.StoreAsync(manga, $"{manga.Id}"),
            SourceObject source => session.StoreAsync(source, source.Id),
            UserObject user     => session.StoreAsync(user, user.Id),
            _                   => throw new ArgumentOutOfRangeException(nameof(item), item, null)
        };

        await task;
        await session.SaveChangesAsync();
    }

    public async Task BulkStoreAsync<T>(IReadOnlyCollection<T> items) {
        BulkInsertOperation? bulkInsert = null;
        try {
            bulkInsert = documentStore.BulkInsert();
            foreach (var item in items) {
                var id = item switch {
                    MangaObject manga   => $"{manga.Id}",
                    SourceObject source => source.Id,
                    _                   => null
                };
                await bulkInsert.StoreAsync(item, id);
            }
        }
        finally {
            if (bulkInsert != null) await bulkInsert.DisposeAsync();
        }
    }
}