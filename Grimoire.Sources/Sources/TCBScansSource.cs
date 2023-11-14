using AngleSharp.Html.Dom;
using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;

namespace Grimoire.Sources.Sources;

public sealed class TCBScansSource(HtmlParser htmlParser) : IGrimoireSource {
    public string Name
        => "TCB Scans";

    public string Url
        => "https://tcbscans.com";

    public string Icon
        => $"{Url}/files/apple-touch-icon.png";

    public async Task<IReadOnlyList<Manga>> GetMangasAsync() {
        using var document = await htmlParser.ParseAsync($"{Url}/projects");
        var tasks = document
            .QuerySelectorAll("a.mb-3.text-white")
            .AsParallel()
            .Select(x => GetMangaAsync($"{Url}{x.As<IHtmlAnchorElement>().PathName}"));
        return await Task.WhenAll(tasks);
    }

    public async Task<Manga> GetMangaAsync(string url) {
        using var document = await htmlParser.ParseAsync(url);
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
                    Url = $"{Url}{(c as IHtmlAnchorElement).PathName}"
                })
                .ToArray()
        };
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        using var document = await htmlParser.ParseAsync(chapter.Url);
        chapter.Pages = document
            .QuerySelectorAll("img.fixed-ratio-content")
            .Select(x => (x as IHtmlImageElement).Source)
            .ToArray();
        return chapter;
    }
}