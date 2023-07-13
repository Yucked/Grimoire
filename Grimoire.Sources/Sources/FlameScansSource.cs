using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
using Grimoire.Sources.Handler;
using Grimoire.Sources.Miscellaneous;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class FlameScansSource : BaseWordPressSource, IGrimoireSource {
    public override string Name
        => "Flame Scans";

    public string BaseUrl
        => "https://flamescans.org";

    public string Icon
        => $"{BaseUrl}/favicon.ico";

    private readonly ILogger<FlameScansSource> _logger;

    public FlameScansSource(HttpClient httpClient, ILogger<FlameScansSource> logger)
        : base(httpClient, logger) {
        _logger = logger;
    }

    public Task<IReadOnlyList<Manga>> FetchMangasAsyncs() {
        return FetchMangasAsync(BaseUrl, "series/list-mode", "div.main-info");
    }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await Misc.ParseAsync($"{BaseUrl}/manga/list-mode");
        var results = document
            .QuerySelectorAll("div.soralist > * a.series")
            .AsParallel()
            .Select(x => GetMangaAsync((x as IHtmlAnchorElement).Href));
        return await Task.WhenAll(results);
    }

    public async Task<Manga> GetMangaAsync(string url) {
        using var document = await Misc.ParseAsync(url);
        var titleElement = document.GetElementById("titlemove");

        _logger.LogInformation("Fetching information for: {}", titleElement.Children[0].TextContent);
        var manga = new Manga {
            Name = titleElement.Children[0].TextContent,
            Url = url,
            SourceId = Name.GetIdFromName(),
            LastFetch = DateTimeOffset.Now,
            Cover = document.QuerySelector("img.wp-post-image").As<IHtmlImageElement>().Source,
            Chapters = ParseChapters(document).ToArray()
        };

        try {
            manga.Metonyms = titleElement
                .GetElementsByClassName("alternative")
                .FirstOrDefault()
                ?.TextContent
                .Slice(Separators);

            manga.Summary = document.QuerySelector("*[itemprop='description']")
                !.Descendents<IHtmlParagraphElement>()
                .Select(x => x.TextContent.Clean().Trim())
                .Join();

            manga.Genre = document
                .QuerySelector("div.wd-full > span.mgen")
                ?.TextContent
                .Slice(' ');

            manga.Author = document
                .QuerySelectorAll("div.tsinfo > div.imptdt")
                .FirstOrDefault(x => x.TextContent.Clean().Trim()[..6] == "Author")
                ?.TextContent
                .Slice(' ')[1..]
                .Join()
                .Clean()
                ?.Trim();
        }
        catch (Exception exception) {
            _logger.LogError("{}: {}\n{}\n{}",
                manga.Name,
                manga.Url,
                exception.Message,
                exception);
        }

        return manga;
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