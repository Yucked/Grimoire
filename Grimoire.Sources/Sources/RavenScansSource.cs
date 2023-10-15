using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
using Grimoire.Sources.Helpers;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

// "img.ts-main-image"
public class RavenScansSource(
    ILogger<ArenaScansSource> logger,
    HtmlParser htmlParser) : IGrimoireSource {
    public string Name
        => "Raven Scans";

    public string Url
        => "https://ravenscans.com";

    public string Icon
        => "https://i0.wp.com/ravenscans.com/wp-content/uploads/2022/12/cropped-33.png";

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