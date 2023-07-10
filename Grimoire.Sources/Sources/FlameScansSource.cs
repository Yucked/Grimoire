using Grimoire.Sources.Handler;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class FlameScansSource : BaseWordPressSource, IGrimoireSource {
    public override string Name
        => "Flame Scans";

    public string BaseUrl
        => "https://flamescans.org";

    public string Icon
        => $"{BaseUrl}/favicon.ico";

    public FlameScansSource(HttpClient httpClient, ILogger<FlameScansSource> logger)
        : base(httpClient, logger) { }

    public Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        return FetchMangasAsync(BaseUrl, "series/list-mode", "div.main-info");
    }

    public Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotSupportedException("Data is fetched via list mode.");
    }

    public Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        return base.FetchChaptersAsync(manga.Url);
    }

    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return base.FetchChapterAsync(chapter, "img.alignnone");
    }
}