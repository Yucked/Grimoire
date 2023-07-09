using Grimoire.Sources.Handler;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class AsuraScansSource : BaseWordPressSource, IGrimoireSource {
    public override string Name
        => "Asura Scans";

    public string BaseUrl
        => "https://www.asurascans.com";

    public string Icon
        => $"{BaseUrl}/wp-content/uploads/2021/03/Group_1.png";

    public AsuraScansSource(HttpClient httpClient, ILogger<AsuraScansSource> logger)
        : base(httpClient, logger) { }

    public Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        return FetchFetchFetchAsync(BaseUrl, "manga/list-mode", "div.bigcontent");
    }

    public Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotSupportedException("");
    }

    public Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        return base.FetchChaptersAsync(manga.Url);
    }

    public Task<Chapter> FetchChapterAsync(Chapter chapter) {
        return base.FetchChapterAsync(chapter, "img.alignnone");
    }
}