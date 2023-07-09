using Grimoire.Sources.Handler;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public sealed class ArenaScansSource : BaseWordPressSource, IGrimoireSource {
    public override string Name
        => "Arena Scans";

    public string BaseUrl
        => "https://arenascans.net";

    public string Icon
        => $"{BaseUrl}/favicon.ico";

    public ArenaScansSource(HttpClient httpClient, ILogger<ArenaScansSource> logger)
        : base(httpClient, logger) { }

    public Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        return FetchMangasAsync(BaseUrl, "manga/list-mode", "div.main-info");
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