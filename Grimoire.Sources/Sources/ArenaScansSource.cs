using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class ArenaScansSource : IGrimoireSource {
    public string Name
        => "Arena Scans";

    public string Url
        => "https://arenascans.net";

    public string Icon
        => $"{Url}/favicon.ico";

    private readonly ILogger<ArenaScansSource> _logger;
    private readonly HtmlParser _htmlParser;

    protected static readonly char[] Separators = { ',', '|' };
    protected static readonly string[] AltStrings = { "Alternative Titles", "desktop-titles" };

    public ArenaScansSource(ILogger<ArenaScansSource> logger, HtmlParser htmlParser) {
        _logger = logger;
        _htmlParser = htmlParser;
    }

    public async Task<IReadOnlyList<Manga>> GetMangasAsync() {
        using var document = await _htmlParser.ParseAsync($"{Url}/manga/list-mode");
        var results = document
            .QuerySelectorAll("div.soralist > * a.series")
            .AsParallel()
            .Select(x => GetMangaAsync((x as IHtmlAnchorElement).Href));
        return await Task.WhenAll(results);
    }

    public async Task<Manga> GetMangaAsync(string url) {
        using var document = await _htmlParser.ParseAsync(url);

        _logger.LogInformation("Fetching information for: {}", url);
        var manga = new Manga {
            Name = document.QuerySelector("h1.entry-title[itemprop='name']").TextContent,
            Url = url,
            SourceId = Name.GetIdFromName(),
            LastFetch = DateTimeOffset.Now,
            Cover = document.QuerySelector("img.wp-post-image").As<IHtmlImageElement>().Source,
            Chapters = document.GetElementById("chapterlist")
                .FirstChild
                .ChildNodes
                .Where(x => x is IHtmlListItemElement)
                .Select(x => {
                    var element = x as IHtmlElement;
                    return new Chapter {
                        Name = element.GetElementsByClassName("chapternum").FirstOrDefault().TextContent.Clean(),
                        Url = x.FindDescendant<IHtmlAnchorElement>().Href,
                        ReleasedOn = DateOnly.Parse(
                            element.GetElementsByClassName("chapterdate").FirstOrDefault().TextContent)
                    };
                })
                .ToArray()
        };

        try {
            manga.Metonyms = document
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

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        try {
            using var document = await _htmlParser.ParseAsync(chapter.Url);
            var chapterId = document.Head
                .Descendents<IHtmlLinkElement>()
                .First(x =>
                    x.Type == "application/json" &&
                    x.Relation == "alternate")
                .Href
                .Split('/')[^1];

            async Task<IDocument> GetChapterDocumentAsync() {
                var content =
                    await _htmlParser.GetContentAsync(
                        $"{Url}/wp-json/wp/v2/posts/{chapterId}", true);
                await using var stream = await content.ReadAsStreamAsync();
                using var jsonDocument = await JsonDocument.ParseAsync(stream);
                var html = jsonDocument.RootElement
                    .GetProperty("content")
                    .GetProperty("rendered")
                    .GetString();
                return await _htmlParser.ParseHtmlAsync(html);
            }

            var parsedChapters = document
                .GetElementById("readerarea")!
                .Descendents<IHtmlImageElement>()
                .Select(x => x.Source)
                .ToArray();

            var htmlChapters = (await GetChapterDocumentAsync())
                .Descendents<IHtmlImageElement>()
                .Select(x => x.Source)
                .ToArray();

            chapter.Pages = htmlChapters.Length > parsedChapters.Length
                ? htmlChapters
                : parsedChapters;

            return chapter;
        }
        catch (Exception exception) {
            _logger.LogError("{}: {}\n{}\n{}",
                chapter.Name,
                chapter.Url,
                exception.Message,
                exception);
            throw;
        }
    }
}