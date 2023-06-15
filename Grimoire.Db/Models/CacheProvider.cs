using Grimoire.Providers.Models;

namespace Grimoire.Db.Models;

public record CacheProvider {
    /// <summary>
    /// PROVIDER_NAME
    /// </summary>
    public string Id { get; init; }

    public string IconPath { get; init; }
    public IReadOnlyList<Manga> Mangas { get; init; }
}