using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Handler;

public class BaseWordPressSource {
    protected readonly HttpClient HttpClient;
    private readonly ILogger<BaseWordPressSource> _logger;

    protected BaseWordPressSource(HttpClient httpClient, ILogger<BaseWordPressSource> logger) {
        HttpClient = httpClient;
        _logger = logger;
    }

    protected static IReadOnlyList<Chapter> ParseWordPressChapters(IDocument document) {
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

    protected async Task<IReadOnlyList<Manga>> FetchMangasAsync(string baseUrl,
                                                                string path,
                                                                string selector) {
        using var document = await HttpClient.ParseAsync($"{baseUrl}/{path}");
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

                try {
                    using var doc = await HttpClient.ParseAsync(manga.Url);
                    var infoDiv = doc.QuerySelector(selector);

                    manga.Cover = infoDiv.FindDescendant<IHtmlImageElement>(2).Source;
                    manga.Metonyms = infoDiv.Find<IHtmlSpanElement>("B", "Alternative")?.Split(',');
                    manga.Summary = infoDiv.Find<IHtmlParagraphElement>("H2", "Synopsis")?.TextContent;
                    manga.Author = infoDiv.Find<IHtmlSpanElement>("B", "Author")?.TextContent.Clean();
                    manga.Genre = infoDiv.Find<IHtmlSpanElement>("B", "Genres")?.Split(' ');
                    manga.Chapters = ParseWordPressChapters(doc).ToArray();
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
            using var document = await HttpClient.ParseAsync(mangaUrl);
            return ParseWordPressChapters(document);
        }
        catch (Exception exception) {
            _logger.LogError("{exception}\n{message}", exception, exception.Message);
            throw;
        }
    }

    internal async Task<Chapter> FetchChapterAsync(Chapter chapter, string selector) {
        try {
            using var document = await HttpClient.ParseAsync(chapter.Url);
            chapter.Pages = document.QuerySelectorAll(selector)!
                .Select((x, index) => new {
                    Key = index, Value = (x as IHtmlImageElement).Source
                })
                .ToDictionary(x => x.Key, x => x.Value);
            return chapter;
        }
        catch (Exception exception) {
            _logger.LogError("{exception}\n{message}", exception, exception.Message);
            throw;
        }
    }
}