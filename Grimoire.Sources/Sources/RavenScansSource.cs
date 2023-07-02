using Grimoire.Sources.Handler;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class RavenScansSource : BaseWordPressSource, IGrimoireSource {
    public string Name
        => "Raven Scans";

    public string BaseUrl
        => "https://ravenscans.com";

    public string Icon
        => "https://i0.wp.com/ravenscans.com/wp-content/uploads/2022/12/cropped-33.png";
    
    public RavenScansSource(HttpClient httpClient, ILogger<RavenScansSource> logger)
        : base(httpClient, logger) { }

    public Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        return base.FetchFetchFetchAsync(BaseUrl, "manga/list-mode", "div.bigcontent");
    }

    public Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotSupportedException("Data is fetched via list mode.");
    }

    public Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        return base.FetchChaptersAsync(manga.Url);
    }

    // TODO: Requires a delay
    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return base.FetchChapterAsync(chapter, "img.ts-main-image");
    }
}