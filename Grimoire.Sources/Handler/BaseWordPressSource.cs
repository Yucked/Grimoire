using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Handler;

public abstract class BaseWordPressSource {
    private readonly ILogger<BaseWordPressSource> _logger;

    protected static readonly char[] Separators = {
        ',',
        '|'
    };

    private static readonly string[] AltStrings = {
        "Alternative Titles",
        "desktop-titles"
    };

    private readonly HttpClient _httpClient;

    public abstract string Name { get; }

    protected BaseWordPressSource(HttpClient httpClient,
                                  ILogger<BaseWordPressSource> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    protected async Task<IReadOnlyList<Manga>> FetchMangasAsync(string baseUrl,
                                                                    string path,
                                                                    string selector) {
        using var document = await Misc.ParseAsync($"{baseUrl}/{path}");
        var results = document
            .QuerySelectorAll("div.soralist > * a.series")
            .AsParallel()
            .Select(async x => {
                var manga = new Manga {
                    Name = x.TextContent,
                    Url = (x as IHtmlAnchorElement).Href,
                    SourceId = Name.GetIdFromName(),
                    LastFetch = DateTimeOffset.Now
                };
                _logger.LogInformation("Fetching information for: {}", manga.Name);

                try {
                    using var doc = await Misc.ParseAsync(manga.Url);
                    var info = doc.QuerySelector(selector)!
                        .Descendents<IElement>()
                        .ToArray();

                    manga.Cover = doc.QuerySelector("img.wp-post-image").As<IHtmlImageElement>().Source;

                    // TODO: Arena Scans
                    manga.Author = (info
                            .First(v => v.TextContent.Trim() == "Author")
                            .Parent as IHtmlDivElement)
                        .LastElementChild!
                        .TextContent
                        .Clean()
                        .Trim();

                    manga.Genre = info
                        .Where(v =>
                            v.HasAttribute("rel") &&
                            v.GetAttribute("rel") == "tag")
                        .Select(v => v.TextContent)
                        .ToArray();

                    manga.Summary = string.Join(" ", info
                        .First(v =>
                            v.HasAttribute("itemprop") &&
                            v.GetAttribute("itemprop") == "description")
                        .ChildNodes
                        .Select(v => v.TextContent.Clean().Trim()));

                    manga.Metonyms = (info
                            .FirstOrDefault(v =>
                                AltStrings.Any(a => a == v.TextContent) ||
                                AltStrings.Any(a => a == v.ClassName)
                            )
                            ?.Parent as IHtmlDivElement)
                        ?.LastElementChild
                        ?.TextContent
                        .Slice(Separators);

                    manga.Chapters = ParseChapters(doc).ToArray();
                }
                catch (Exception exception) {
                    _logger.LogError("{}: {}\n{}\n{}",
                        manga.Name,
                        manga.Url,
                        exception.Message,
                        exception);
                }

                return manga;
            });

        return await Task.WhenAll(results);
    }

    protected async Task<IReadOnlyList<Chapter>> FetchChaptersAsync(string mangaUrl) {
        try {
            using var document = await Misc.ParseAsync(mangaUrl);
            return ParseChapters(document);
        }
        catch (Exception exception) {
            _logger.LogError("{exception}\n{message}", exception, exception.Message);
            throw;
        }
    }

    protected async Task<Chapter> FetchChapterAsync(Chapter chapter, string selector) {
        try {
            using var document = await Misc.ParseAsync(chapter.Url);
            var chapterId = document.Head
                .Descendents<IHtmlLinkElement>()
                .First(x =>
                    x.Type == "application/json" &&
                    x.Relation == "alternate")
                .Href
                .Split('/')[^1];

            var chapterDoc = await _httpClient.FetchChapterHTMLAsync(document.Head.BaseUrl.Origin, chapterId);

            var parsedChapters = document
                .GetElementById("readerarea")!
                .Descendents<IHtmlImageElement>()
                .Select(x => x.Source)
                .ToArray();

            var htmlChapters = chapterDoc
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

    protected static IReadOnlyList<Chapter> ParseChapters(IDocument document) {
        return document.GetElementById("chapterlist")
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
            .ToArray();
    }
}