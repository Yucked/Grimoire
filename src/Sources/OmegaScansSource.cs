using Grimoire.Handlers;
using Grimoire.Models;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

public class OmegaScansSource : IGrimoireSource {
    public string Name
        => "Omega Scans";

    public string Url
        => "https://api.omegascans.org";

    public string Icon
        => "https://omegascans.org/icon.png";

    private readonly HttpHandler _httpHandler;

    public async Task<IReadOnlyList<Manga>> GetMangasAsync() {
        throw new NotImplementedException();
    }

    public async Task<Manga> GetMangaAsync(string url) {
        throw new NotImplementedException();
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        throw new NotImplementedException();
    }
}