using System.Globalization;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Miscellaneous;
using Grimoire.Sources.Models;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public sealed class Manhwa18NetSource : IGrimoireSource {
    public string Name
        => "Manhwa 18";

    public string BaseUrl
        => "https://manhwa18.net";

    public string Icon
        => $"{BaseUrl}/favicon1.ico";

    private readonly HttpClient _httpClient;
    private readonly ILogger<Manhwa18NetSource> _logger;

    public Manhwa18NetSource(HttpClient httpClient, ILogger<Manhwa18NetSource> logger) {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Manga>> FetchMangasAsync() {
        using var document = await Misc.ParseAsync($"{BaseUrl}/manga-list");
        var lastPage = int.Parse((document
            .GetElementsByClassName("paging_item paging_prevnext next")
            .FirstOrDefault() as IHtmlAnchorElement).Href[^2..]);
        _logger.LogDebug("Total pages: {pages}", lastPage);

        var results = await Task.WhenAll(Enumerable
            .Range(1, lastPage)
            .Select(PaginateAsync));

        var mangas = new List<Manga>();
        await Parallel.ForEachAsync(results.SelectMany(x => x), async (manga, _) => {
            _logger.LogDebug("Getting additional information for {manga}", manga.Name);
            using var doc = await Misc.ParseAsync(manga.Url);
            var extras = doc.GetElementsByClassName("info-item");

            manga.Author = GetTagData(extras, "Author");
            manga.Genre = GetTagData(extras, "Genre")
                .Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
            manga.Summary = doc.GetElementsByClassName("summary-content").FirstOrDefault()?.TextContent;
            manga.Chapters = doc
                .GetElementsByClassName("list-chapters at-series")
                .FirstOrDefault()
                ?.Children
                .Select(x => {
                    var href = x as IHtmlAnchorElement;
                    var parsedDate = DateOnly.ParseExact(href
                        .GetElementsByClassName("chapter-time")
                        .FirstOrDefault()
                        .TextContent.Split('-')[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    return new Chapter {
                        Name = href.Title,
                        Url = href.Href,
                        ReleasedOn = parsedDate
                    };
                }).ToArray();
            manga.Metonyms = new[] {
                GetTagData(extras, "Other name"),
                GetTagData(extras, "Doujinshi")
            };

            mangas.Add(manga);
        });

        return mangas;
    }

    public async Task<IReadOnlyList<Manga>> PaginateAsync(int page) {
        using var document = await Misc.ParseAsync($"{BaseUrl}/manga-list?page={page}");
        var titles = document.GetElementsByClassName("thumb-item-flow col-6 col-md-3");
        _logger.LogDebug("Parsing page #{page} with {titlesCount} titles", page, titles.Length);

        return titles
            .AsParallel()
            .Select(x => {
                var info = x.Children[1].Children[0] as IHtmlAnchorElement;
                return new Manga {
                    Name = info?.Title!,
                    Url = info?.Href!,
                    Cover = x
                        .GetElementsByClassName("content img-in-ratio lazyloaded")
                        .First()
                        .GetAttribute("data-bg"),
                    LastFetch = DateTimeOffset.Now,
                    SourceName = GetType().Name[..^6]
                };
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<Chapter>> FetchChaptersAsync(Manga manga) {
        using var document = await Misc.ParseAsync(manga.Url);
        _logger.LogInformation("Fetching chapters for {name}", manga.Name);

        return document
            .GetElementsByClassName("list-chapters at-series")
            .FirstOrDefault()
            .Children
            .Select(x => {
                var href = x as IHtmlAnchorElement;
                var parsedDate = DateOnly.ParseExact(href
                    .GetElementsByClassName("chapter-time")
                    .FirstOrDefault()
                    .TextContent.Split('-')[1].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                return new Chapter {
                    Name = href.Title,
                    Url = href.Href,
                    ReleasedOn = parsedDate
                };
            }).ToArray();
    }

    public async Task<Chapter> FetchChapterAsync(Chapter chapter) {
        using var document = await Misc.ParseAsync(chapter.Url);
        IElement element;
        do {
            element = document.All.FirstOrDefault(x => x.LocalName == "div" && x.Id == "chapter-content");
        } while (element == default && element.Children.Length == 0);

        chapter.Pages = element
            .Children
            .Select((x, index) => new {
                Key = index, x.Attributes[1].Value
            })
            .ToDictionary(x => x.Key, x => x.Value);
        return chapter;
    }

    private static string GetTagData(IEnumerable<IElement> elements, string tagName) {
        var element = elements.FirstOrDefault(x => x.TextContent.Contains($"{tagName}:"));
        if (element == null) {
            return "N/A";
        }

        var split = element.TextContent.Split(':');
        return split.Length < 2 ? "N/A" : split[1].Trim();
    }
}