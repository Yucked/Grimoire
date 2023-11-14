using System.Text.Json;
using Grimoire.Handlers;
using Grimoire.Models;
using Grimoire.Sources.Interfaces;

namespace Grimoire.Sources;

public class OmegaScansSource(
    HttpHandler httpHandler,
    ILogger<OmegaScansSource> logger) : IGrimoireSource {
    public string Name
        => "Omega Scans";

    public string Url
        => "https://api.omegascans.org";

    public string Icon
        => "https://omegascans.org/icon.png";

    private readonly Dictionary<string, (string Name, string Cover, string Genre)> _apiCache = new();

    public async Task<IReadOnlyList<Manga>> GetMangasAsync() {
        var stream =
            await httpHandler.GetStreamAsync($"{Url}/query?visibility=Public&series_type=All&perPage=100");
        using var document = await JsonDocument.ParseAsync(stream!);
        if (!document.RootElement.TryGetProperty("data", out var dataElement)) {
            logger.LogError("Unable to fetch JSON data from source!");
            return null;
        }

        var tasks = dataElement
            .EnumerateArray()
            .AsParallel()
            .Select(x => {
                var slug = x.GetProperty("series_slug").GetString()!;
                _apiCache.Add(slug,
                    (x.GetProperty("title").GetString()!,
                     x.GetProperty("thumbnail").GetString()!,
                     x.GetProperty("series_type").GetString()!));

                return GetMangaAsync($"https://omegascans.org/series/{slug}");
            });

        var mangas = await Task.WhenAll(tasks);
        _apiCache.Clear();
        return mangas;
    }

    public async Task<Manga> GetMangaAsync(string url) {
        var document = await httpHandler.ParseAsync(url);
        var manga = new Manga {
            Summary = document
                .QuerySelector("div.bg-gray-800 > p")
                .TextContent
        };

        return manga;
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        throw new NotImplementedException();
    }
}