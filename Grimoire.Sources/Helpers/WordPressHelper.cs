using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Commons;
using Grimoire.Commons.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Helpers;

public sealed class WordPressHelper {
    private readonly ILogger _logger;
    private readonly HtmlParser _htmlParser;
    private readonly string _name;
    private readonly string _url;

    private static readonly char[] Separators = { ',', '|' };
    //private static readonly string[] AltStrings = { "Alternative Titles", "desktop-titles" };

    public static WordPressHelper Helper(ILogger logger,
                                         HtmlParser htmlParser,
                                         string name,
                                         string url)
        => new(logger, htmlParser, name, url);

    private WordPressHelper(ILogger logger,
                            HtmlParser htmlParser,
                            string name,
                            string url) {
        _logger = logger;
        _htmlParser = htmlParser;
        _name = name;
        _url = url;
    }

    public async Task<IReadOnlyList<Manga>> GetMangasAsync(string listType = "manga") {
        using var document = await _htmlParser.ParseAsync($"{_url}/{listType}/list-mode");
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
            SourceId = _name.GetIdFromName(),
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
                !.Descendants()
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
                .Descendants<IHtmlLinkElement>()
                .First(x =>
                    x.Type == "application/json" &&
                    x.Relation == "alternate")
                .Href
                .Split('/')[^1];

            var parsedChapters = document
                .GetElementById("readerarea")!
                .Descendants<IHtmlImageElement>()
                .Select(x => x.Source)
                .ToArray();

            var htmlChapters = (await GetChapterDocumentAsync())
                .Descendants<IHtmlImageElement>()
                .Select(x => x.Source)
                .ToArray();

            chapter.Pages = htmlChapters.Length > parsedChapters.Length
                ? htmlChapters
                : parsedChapters;

            return chapter;

            async Task<IDocument> GetChapterDocumentAsync() {
                var content =
                    await _htmlParser.GetContentAsync($"{_url}/wp-json/wp/v2/posts/{chapterId}");
                await using var stream = await content.ReadAsStreamAsync();
                using var jsonDocument = await JsonDocument.ParseAsync(stream);
                var html = jsonDocument.RootElement
                    .GetProperty("content")
                    .GetProperty("rendered")
                    .GetString();
                return await _htmlParser.ParseHtmlAsync(html);
            }
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