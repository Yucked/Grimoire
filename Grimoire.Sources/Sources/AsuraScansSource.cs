using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
using Grimoire.Sources.Helpers;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

// "img.alignnone"
public class AsuraScansSource(
    ILogger<AsuraScansSource> logger,
    HtmlParser htmlParser) : IGrimoireSource {
    public string Name
        => "Asura Scans";

    public string Url
        => "https://asuratoon.com";

    public string Icon
        => $"{Url}/wp-content/uploads/2021/03/Group_1.png";

    public Task<IReadOnlyList<Manga>> GetMangasAsync() {
        return WordPressHelper
            .Helper(logger, htmlParser, Name, Url)
            .GetMangasAsync();
    }

    public Task<Manga> GetMangaAsync(string url) {
        return WordPressHelper
            .Helper(logger, htmlParser, Name, Url)
            .GetMangaAsync(url);
    }

    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return WordPressHelper
            .Helper(logger, htmlParser, Name, Url)
            .FetchChapterAsync(chapter);
    }
}