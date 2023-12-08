using System.Globalization;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Handlers;
using Grimoire.Helpers;
using Grimoire.Models;

namespace Grimoire.Sources.Abstractions;

// Websites based off of NhanMa5213 design
public class HanmaAbstraction {
    private readonly ILogger _logger;
    private readonly HttpHandler _httpHandler;
    private readonly DatabaseHandler _databaseHandler;
    private readonly string _name;
    private readonly string _url;

    public static HanmaAbstraction Helper(ILogger logger,
                                          HttpHandler httpHandler,
                                          DatabaseHandler databaseHandler,
                                          string name,
                                          string url) {
        return new HanmaAbstraction(logger, httpHandler, databaseHandler, name, url);
    }

    private HanmaAbstraction(ILogger logger,
                             HttpHandler httpHandler,
                             DatabaseHandler databaseHandler,
                             string name,
                             string url) {
        _logger = logger;
        _httpHandler = httpHandler;
        _databaseHandler = databaseHandler;
        _name = name;
        _url = url;
    }

    public async Task<IReadOnlyList<Manga>> GetMangasAsync() {
        using var document = await _httpHandler.ParseAsync($"{_url}/manga-list");
        var lastPage = int.Parse(document
            .QuerySelector("a.paging_prevnext.next")!
            .As<IHtmlAnchorElement>().Href[^2..]);

        var urls = await Enumerable
            .Range(1, lastPage)
            .Select(async page => {
                using var doc = await _httpHandler.ParseAsync($"{_url}/manga-list?page={page}");
                return doc
                    .QuerySelectorAll("div.thumb_attr.series-title > a")
                    .Select(x => x.As<IHtmlAnchorElement>().Href);
            })
            .AwaitAsync();

        var mangas = await urls
            .AsParallel()
            .SelectMany(x => x)
            .Select(GetMangaAsync)
            .AwaitAsync();

        return mangas
            .GroupBy(x => x.Id)
            .Select(x => {
                if (x.Count() == 1) {
                    return x.First();
                }

                var lst = x.ToArray();
                return lst[0].Chapters.Count == lst[1].Chapters.Count &&
                       lst[0].Genre.Count > lst[1].Genre.Count ||
                       lst[0].Chapters.Count > lst[1].Chapters.Count
                    ? lst[0]
                    : lst[1];
            })
            .ToArray();
    }

    public async Task<Manga> GetMangaAsync(string url) {
        using var document = await _httpHandler.ParseAsync(url);

        try {
            var manga = new Manga {
                Author = GetInfoValue("Author"),
                Name = document
                    .QuerySelector("span.series-name > a")!
                    .TextContent,
                Summary = document
                    .QuerySelector("div.summary-content")!
                    .TextContent
                    .Clean(),
                Cover = document
                    .QuerySelector("div.img-in-ratio")!
                    .TextContent,
                Genre = GetInfoValue("Genre").Split(' '),
                SourceId = _name.GetIdFromName(),
                Metonyms = new[] {
                    GetInfoValue("Other name"),
                    GetInfoValue("Doujinshi")
                },
                Chapters = document
                    .QuerySelectorAll("ul.list-chapters > a")
                    .Select(x => new Chapter {
                        Url = x.As<IHtmlAnchorElement>().Href,
                        Name = x
                            .QuerySelector("div.chapter-name")!
                            .TextContent,
                        ReleasedOn = DateOnly.ParseExact(
                            x
                                .QuerySelector("div.chapter-time")!
                                .TextContent
                                .Split('-')[1]
                                .Trim(),
                            "dd/MM/yyyy",
                            CultureInfo.InvariantCulture)
                    })
                    .ToArray()
            };

            await _databaseHandler.AddMangaAsync(manga);
            return manga;
        }
        catch (Exception exception) {
            _logger.LogError("{}\n{}", url, exception);
            throw;
        }

        string GetInfoValue(string infoName) {
            var infoElement = document
                .QuerySelectorAll("span.info-name")
                .FirstOrDefault(x => x.TextContent == $"{infoName}:");
            if (infoElement == null) {
                return string.Empty;
            }

            return infoElement
                .ParentElement
                ?.QuerySelector("span.info-value")!
                .TextContent!;
        }
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        using var document = await _httpHandler.ParseAsync(chapter.Url);
        IElement element;
        do {
            element = document
                .All
                .First(x => x is { LocalName: "div", Id: "chapter-content" });
        } while (element.Children.Length == 0);

        chapter.Pages = element
            .Children
            .Select(x => x.Attributes[1]!.Value)
            .ToArray();
        return chapter;
    }
}