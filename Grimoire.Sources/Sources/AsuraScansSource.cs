using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Sources.Handler;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class AsuraScansSource : BaseWordPressSource, IGrimoireSource {
    public string Name
        => "Asura Scans";

    public string BaseUrl
        => "https://www.asurascans.com";

    public string Icon
        => $"{BaseUrl}/wp-content/uploads/2021/03/Group_1.png";

    public AsuraScansSource(HttpClient httpClient, ILogger<AsuraScansSource> logger)
        : base(httpClient, logger) { }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await HttpClient.ParseAsync($"{BaseUrl}/manga/list-mode/", true);
        var results = document
            .QuerySelectorAll("a.series")
            .AsParallel()
            .Select(async x => {
                var manga = new Manga {
                    Name = x.TextContent,
                    Url = (x as IHtmlAnchorElement).Href,
                    SourceName = GetType().Name[..^6],
                    LastFetch = DateTimeOffset.Now
                };

                using var doc = await HttpClient.ParseAsync(manga.Url);
                manga.Cover = doc.QuerySelector("img.wp-post-image").As<IHtmlImageElement>().Source;
                var info = doc.QuerySelector("div.infox");
                var summary = info
                    .Descendents()
                    .First(n => n.NodeName == "H2" &&
                                n.TextContent.Contains("Synopsis")
                    )
                    .ParentElement
                    .FindDescendant<IHtmlParagraphElement>();

                return manga;
            });

        return await Task.WhenAll(results);
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