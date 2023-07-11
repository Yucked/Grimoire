using AngleSharp.Html.Dom;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public sealed class TCBScansSource : IGrimoireSource {
    public string Name
        => "TCB Scans";

    public string BaseUrl
        => "https://tcbscans.com";

    public string Icon
        => $"{BaseUrl}/files/apple-touch-icon.png";

    private readonly ILogger<TCBScansSource> _logger;

    public TCBScansSource(ILogger<TCBScansSource> logger) {
        _logger = logger;
    }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await Misc.ParseAsync($"{BaseUrl}/projects");
        var tasks = document
            .QuerySelectorAll("a.mb-3.text-white")
            .AsParallel()
            .Select(x => GetMangaAsync(x.As<IHtmlAnchorElement>().Href));
        return await Task.WhenAll(tasks);
    }

    public async Task<Manga> GetMangaAsync(string url) {
        using var document = await Misc.ParseAsync(url);
        return new Manga {
            Name = document.QuerySelector("div.px-4 > h1").TextContent.Clean(),
            Url = url,
            Cover = document.QuerySelector("div.flex > img").As<IHtmlImageElement>().Source,
            LastFetch = DateTimeOffset.Now,
            SourceId = Name.GetIdFromName(),
            Summary = document.QuerySelector("p.leading-6")
                .TextContent
                .Clean(),
            Chapters = document.QuerySelectorAll("a.block.border")
                .Select(c => new Chapter {
                    Name = c.TextContent,
                    Url = $"{BaseUrl}{(c as IHtmlAnchorElement).PathName}"
                })
                .ToArray()
        };
    }

    public Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotSupportedException("Source doesn't have pagination.");
    }

    public async Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        using var document = await Misc.ParseAsync(manga.Url);
        _logger.LogDebug("Fetching chapters for {name}", manga.Name);
        return document.GetElementsByClassName("block border border-border bg-card mb-3 p-3 rounded")
            .Select(x => new Chapter {
                Name = x.TextContent,
                Url = $"{BaseUrl}{(x as IHtmlAnchorElement).PathName}"
            })
            .ToArray();
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        using var document = await Misc.ParseAsync(chapter.Url);
        chapter.Pages = document
            .QuerySelectorAll("img.fixed-ratio-content")
            .Select(x => (x as IHtmlImageElement).Source)
            .ToArray();
        return chapter;
    }
}