using Grimoire.Sources.Commons;

namespace Grimoire.Sources.MetadataProviders;

public sealed class MyAnimeListProvider(
    ILogger<MyAnimeListProvider> logger) : IMetadataProvider {
    public Task<MetadataResult?> FindMangaAsync(string title) {
        logger.LogDebug("MyAnimeList provider not yet implemented for {Title}", title);
        return Task.FromResult<MetadataResult?>(null);
    }
}