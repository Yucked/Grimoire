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
}