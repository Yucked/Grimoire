using AngleSharp.Html.Dom;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class FlameScansSource : IGrimoireSource {
    public string Name
        => "Flame Scans";

    public string BaseUrl
        => "https://flamescans.org";

    public string Icon
        => $"{BaseUrl}/favicon.ico";

    private readonly HttpClient _httpClient;
    private readonly ILogger<FlameScansSource> _logger;

    public FlameScansSource(HttpClient httpClient, ILogger<FlameScansSource> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await _httpClient.ParseAsync($"{BaseUrl}/series/list-mode/");
        var results = document.GetElementsByClassName("blix")
            .SelectMany(x => x.Children[1].Children)
            .Select(async x => {
                var manga = new Manga {
                    Name = x.FirstChild.TextContent,
                    Url = (x.FirstChild as IHtmlAnchorElement).Href,
                    Provider = GetType().Name[..^8],
                    LastFetch = DateTimeOffset.Now
                };

                try {
                    using var doc = await _httpClient.ParseAsync(manga.Url);
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
                }
                catch (Exception exception) {
                    _logger.LogError("Failed to parse html for {url}\n{exception}", manga.Url, exception);
                }

                return manga;
            });

        return await Task.WhenAll(results);
    }

    public Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        throw new NotSupportedException("Data is fetched via list mode.");
    }

    public async Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        try {
            using var document = await _httpClient.ParseAsync(manga.Url);
            return document.GetElementById("chapterlist")
                .FirstChild
                .ChildNodes
                .Where(x => x is IHtmlListItemElement)
                .Select(x => {
                    var anchor = (x as IHtmlElement).Children[0] as IHtmlAnchorElement;
                    return new Chapter {
                        Name = anchor.GetElementsByClassName("chapternum").FirstOrDefault().TextContent.Clean(),
                        Url = anchor.Href,
                        ReleasedOn = DateOnly.Parse(
                            anchor.GetElementsByClassName("chapterdate").FirstOrDefault().TextContent)
                    };
                })
                .ToArray();
        }
        catch (Exception exception) {
            _logger.LogError("{exception}\n{message}", exception, exception.Message);
            throw;
        }
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        throw new NotImplementedException();
    }
}