using Grimoire.Objects;
using Raven.Client.Documents;

namespace Grimoire.Handlers;

public sealed partial class DatabaseHandler {
    public async Task<UserObject?> GetUserAsync(string userId) {
        using var session = documentStore.OpenAsyncSession();
        return await session.LoadAsync<UserObject?>(userId);
    }

    public async Task<IReadOnlyList<UserObject>> GetUsersAsync() {
        using var session = documentStore.OpenAsyncSession();
        return await session
            .Query<UserObject>()
            .ToListAsync();
    }

    public async Task UpsertUserAsync(UserObject user) {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(user, user.Id);
        await session.SaveChangesAsync();
    }

    public async Task AddToLibraryAsync(string userId, string sourceId, string mangaId) {
        using var session = documentStore.OpenAsyncSession();
        var user = await session.LoadAsync<UserObject>(userId);
        if (user is null ||
            user.Library.Any(x => x.Key == $"{sourceId}/{mangaId}")) {
            return;
        }

        user.Library.TryAdd($"{sourceId}/{mangaId}", 0);
        await session.SaveChangesAsync();
    }

    public async Task RemoveFromLibraryAsync(string userId, string sourceId, string mangaId) {
        using var session = documentStore.OpenAsyncSession();
        var user = await session.LoadAsync<UserObject>(userId);
        if (user is null) {
            return;
        }

        user.Library.TryRemove($"{sourceId}/{mangaId}", out _);
        await session.SaveChangesAsync();
    }

    public async Task UpdateProgressAsync(string userId, string sourceId, string mangaId, float chapter) {
        using var session = documentStore.OpenAsyncSession();
        var user = await session.LoadAsync<UserObject>(userId);
        if (user is null || !user.Library.TryGetValue($"{sourceId}/{mangaId}", out _)) {
            return;
        }

        user.Library[$"{sourceId}/{mangaId}"] = chapter;
        await session.SaveChangesAsync();
    }

    public async Task RefreshLibraryAsync(string userId, CancellationToken cancellationToken = default) {
        var user = await GetUserAsync(userId);
        if (user is null) {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return;
        }

        var library = user.Library
            .Select(e => e.Key)
            .Distinct()
            .ToList();

        if (library.Count == 0) {
            throw new Exception("User's library is empty.");
        }

        await Parallel.ForEachAsync(library, cancellationToken, async (key, _) => {
            var sourceId = key.Split('/')[0];
            var mangaId = key.Split('/')[1];

            var manga = await GetMangaAsync(sourceId, mangaId);
            if (manga == null) {
                throw new Exception($"Manga {mangaId} not found.");
            }

            var source = sources.FirstOrDefault(x => x.Name.GetIdFromName() == sourceId);
            if (source is null) {
                throw new Exception($"Source {sourceId} not found.");
            }

            var updated = await source.GetMangaAsync(manga.SourceUrl);
            await StoreAsync(updated);
        });
    }
}