using Grimoire.Handlers;
using Grimoire.Models;
using Grimoire.Sources.Abstractions;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

public class Manhwa18NetSource(
    ILogger<Manhwa18NetSource> logger,
    HttpHandler httpHandler,
    DatabaseHandler databaseHandler) : IGrimoireSource {
    public string Name
        => "Manhwa 18";

    public string Url
        => "https://manhwa18.net";

    public string Icon
        => $"{Url}/favicon1.ico";

    public Task<IReadOnlyList<Manga>> GetMangasAsync() {
        return HanmaAbstraction
            .Helper(logger, httpHandler, databaseHandler, Name, Url)
            .GetMangasAsync();
    }

    public Task<Manga> GetMangaAsync(string url) {
        return HanmaAbstraction
            .Helper(logger, httpHandler, databaseHandler, Name, Url)
            .GetMangaAsync(url);
    }

    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return HanmaAbstraction
            .Helper(logger, httpHandler, databaseHandler, Name, Url)
            .FetchChapterAsync(chapter);
    }
}