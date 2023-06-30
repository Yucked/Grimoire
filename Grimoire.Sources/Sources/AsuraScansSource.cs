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
        using var document = await HttpClient.ParseAsync($"{BaseUrl}/?s=", true);
        var lastPage = (document.GetElementsByClassName("page-numbers").Skip(4).FirstOrDefault() as IHtmlAnchorElement)
            .Href[^4..^3];

        var results = await Task.WhenAll(Enumerable
            .Range(1, int.Parse(lastPage))
            .Select(PaginateAsync));

        var populate = results
            .SelectMany(x => x)
            .AsParallel()
            .Select(async manga => {
                using var doc = await HttpClient.ParseAsync(manga.Url);

                // TODO: Summary and Author messing up. Probably better to use Contains to check data
                var info = doc.QuerySelector("div.infox");
                manga.Author = info.Children[3].Children[1].Children[1].TextContent.Clean();
                manga.Summary = info.QuerySelector("div.entry-content").TextContent.Clean();
                manga.Genre = info.QuerySelector("span.mgen").TextContent.Split(' ');

                var addName = info.QuerySelector("div.wd-full > span").TextContent;
                manga.Metonyms = manga.Genre.Count == addName.Split(' ').Length
                    ? default
                    : addName.Split(',');

                manga.Chapters = doc.GetElementsByClassName("eph-num")
                    .Select(x => {
                        var anchor = x.Children[0] as IHtmlAnchorElement;
                        return new Chapter {
                            Name = anchor.Children[0].TextContent,
                            Url = anchor.Href,
                            ReleasedOn = DateOnly.Parse(anchor.Children[1].TextContent)
                        };
                    })
                    .ToArray();
                return manga;
            });

        return await Task.WhenAll(populate);
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