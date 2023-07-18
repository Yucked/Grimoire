using System.Globalization;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Grimoire.Commons.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class Manhwa18NetSource : IGrimoireSource {
    public string Name
        => "Manhwa 18";

    public string Url
        => "https://manhwa18.net";

    public string Icon
        => $"{Url}/favicon1.ico";

    private readonly ILogger<Manhwa18NetSource> _logger;
    private readonly HtmlParser _htmlParser;

    public Manhwa18NetSource(ILogger<Manhwa18NetSource> logger, HtmlParser htmlParser) {
        _logger = logger;
        _htmlParser = htmlParser;
    }

    public async Task<IReadOnlyList<Manga>> GetMangasAsync() {
        using var document = await _htmlParser.ParseAsync($"{Url}/manga-list");
        var lastPage = int.Parse(document
            .QuerySelector("a.paging_prevnext.next")
            .As<IHtmlAnchorElement>().Href[^2..]);

        var urls = await Enumerable
            .Range(1, lastPage)
            .Select(async page => {
                using var doc = await _htmlParser.ParseAsync($"{Url}/manga-list?page={page}");
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
                    return x.FirstOrDefault();
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
        using var document = await _htmlParser.ParseAsync(url);

        string GetInfoValue(string infoName) {
            var infoElement = document
                .QuerySelectorAll("span.info-name")
                .FirstOrDefault(x => x.TextContent == $"{infoName}:");
            if (infoElement == null) {
                return string.Empty;
            }

            return infoElement
                .ParentElement
                ?.QuerySelector("span.info-value")
                ?.TextContent;
        }

        try {
            var aks = document.QuerySelector("div.summary-content");
            var manga = new Manga();
            manga.Author = GetInfoValue("Author");
            manga.Name = document.QuerySelector("span.series-name > a")?.TextContent;
            manga.Summary = document.QuerySelector("div.summary-content").TextContent.Clean();
            manga.Cover = document.QuerySelector("div.img-in-ratio").TextContent;
            manga.Genre = GetInfoValue("Genre").Split(' ');
            manga.Metonyms = new[] {
                GetInfoValue("Other name"),
                GetInfoValue("Doujinshi")
            };

            manga.Chapters = document
                .QuerySelectorAll("ul.list-chapters > a")
                .Select(x => new Chapter {
                    Url = x.As<IHtmlAnchorElement>().Href,
                    Name = x.QuerySelector("div.chapter-name").TextContent,
                    ReleasedOn = DateOnly.ParseExact(
                        x.QuerySelector("div.chapter-time").TextContent.Split('-')[1].Trim(),
                        "dd/MM/yyyy",
                        CultureInfo.InvariantCulture)
                })
                .ToArray();

            return manga;
        }
        catch (Exception exception) {
            var stream = new MemoryStream();
            await document.ToHtmlAsync(stream);
            var text = Encoding.UTF8.GetString(stream.ToArray());
            _logger.LogError("{}\n{}", url, exception);
        }

        return default;
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        using var document = await _htmlParser.ParseAsync(chapter.Url);
        IElement element;
        do {
            element = document.All.FirstOrDefault(x => x.LocalName == "div" && x.Id == "chapter-content");
        } while (element == default && element.Children.Length == 0);

        chapter.Pages = element
            .Children
            .Select(x => x.Attributes[1].Value)
            .ToArray();
        return chapter;
    }
}