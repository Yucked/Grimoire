using AngleSharp.Html.Dom;
using Grimoire.Sources.Handler;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class FlameScansSource : BaseWordPressSource, IGrimoireSource {
    public string Name
        => "Flame Scans";

    public string BaseUrl
        => "https://flamescans.org";

    public string Icon
        => $"{BaseUrl}/favicon.ico";

    public FlameScansSource(HttpClient httpClient, ILogger<FlameScansSource> logger)
        : base(httpClient, logger) { }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await HttpClient.ParseAsync($"{BaseUrl}/series/list-mode/");
        var results = document.GetElementsByClassName("blix")
            .SelectMany(x => x.Children[1].Children)
            .Select(async x => {
                var manga = new Manga {
                    Name = x.FirstChild.TextContent,
                    Url = (x.FirstChild as IHtmlAnchorElement).Href,
                    SourceName = GetType().Name[..^6],
                    LastFetch = DateTimeOffset.Now
                };

                using var doc = await HttpClient.ParseAsync(manga.Url);
                manga.Author = doc.QuerySelectorAll("div.imptdt")
                    .FirstOrDefault(c => c.TextContent.Contains("Author"))
                    .TextContent
                    .Replace("Author", string.Empty)
                    .Trim().Clean();
                manga.Cover = (doc.QuerySelector("div.thumb > img") as IHtmlImageElement).Source;
                manga.Summary = doc.QuerySelector("div.wd-full > div.entry-content").TextContent.Clean();
                manga.Genre = doc.QuerySelector("div.wd-full > span.mgen").TextContent.Split(' ');
                manga.Metonyms = doc.QuerySelector("div.desktop-titles")?.TextContent.Split(',');
                manga.Chapters = doc.GetElementById("chapterlist")
                    .FirstChild
                    .ChildNodes
                    .Where(c => c is IHtmlListItemElement)
                    .Select(c => {
                        var anchor = (c as IHtmlElement).Children[0] as IHtmlAnchorElement;
                        return new Chapter {
                            Name = anchor.GetElementsByClassName("chapternum").FirstOrDefault().TextContent.Clean(),
                            Url = anchor.Href,
                            ReleasedOn = DateOnly.Parse(
                                anchor.GetElementsByClassName("chapterdate").FirstOrDefault().TextContent)
                        };
                    })
                    .ToArray();

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