using Grimoire.Handlers;

namespace Grimoire.Sources.MetadataProviders;

public sealed class MyAnimeListProvider(
    ScrapingHandler scrapingHandler,
    ILogger<MyAnimeListProvider> logger) {
    public async Task GetMangaAsync() {
        
    }
}