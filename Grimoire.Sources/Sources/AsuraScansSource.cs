using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
using Grimoire.Sources.Handler;
using Grimoire.Sources.Miscellaneous;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class AsuraScansSource : BaseWordPressSource, IGrimoireSource {
    public override string Name
        => "Asura Scans";

    public string BaseUrl
        => "https://www.asurascans.com";

    public string Icon
        => $"{BaseUrl}/wp-content/uploads/2021/03/Group_1.png";

    private readonly ILogger<AsuraScansSource> _logger;

    public AsuraScansSource(HttpClient httpClient, ILogger<AsuraScansSource> logger)
        : base(httpClient, logger) {
        _logger = logger;
    }

    public Task<IReadOnlyList<Manga>> FetchMangasAsyncs() {
        return FetchMangasAsync(BaseUrl, "manga/list-mode", "div.bigcontent");
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
        try {
            _logger.LogInformation("Fetching information for: {}", url);
            var manga = new Manga {
                Url = url,
                SourceId = Name.GetIdFromName(),
                LastFetch = DateTimeOffset.Now
            };

            manga.Name = document.QuerySelector("h1.entry-title[itemprop='name']").TextContent;
            manga.Cover = document.QuerySelector("img.wp-post-image").As<IHtmlImageElement>().Source;
            manga.Chapters = ParseChapters(document).ToArray();
            manga.Metonyms = (document
                    .All
                    .FirstOrDefault(v =>
                        AltStrings.Any(a => a == v.TextContent) ||
                        AltStrings.Any(a => a == v.ClassName)
                    )
                    ?.Parent as IHtmlDivElement)
                ?.TextContent
                .Slice(Separators);

            manga.Summary = document.QuerySelector("[itemprop='description']")
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

            return manga;
        }
        catch (Exception exception) {
            var stream = new MemoryStream();
            await document.ToHtmlAsync(stream);
            var text = Encoding.UTF8.GetString(stream.ToArray());
            var count = document
                .Descendents()
                .Count();
            _logger.LogError("{}\n{}\n{}",
                url,
                exception.Message,
                exception);
            throw;
        }
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