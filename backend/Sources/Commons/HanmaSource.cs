using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Handlers;
using Grimoire.Objects;

namespace Grimoire.Sources.Commons;

// Websites based on the NhanMa5213 design
public abstract partial class HanmaSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger logger) : IGrimoireSource {
    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex ChapterNumberRegex();

    [GeneratedRegex("""url\(['"]?([^'"]+)['"]?\)""")]
    private static partial Regex CoverUrlRegex();

    public abstract string Name { get; }
    public abstract string Url { get; }
    public abstract string Icon { get; }

    public async Task<IReadOnlyList<MangaObject>> GetMangasAsync() {
        var firstDoc = await scrapingHandler.GetHtmlDocumentAsync($"{Url}/manga-list");
        var nextHref = firstDoc
            .QuerySelector("a.paging_prevnext.next")
            ?.As<IHtmlAnchorElement>()
            .Href;
        firstDoc.Close();

        if (nextHref is null)
            return [];

        var lastPage = int.Parse(nextHref.Split("page=")[^1]);

        var urlTasks = Enumerable
            .Range(1, lastPage)
            .Select(async page => {
                var doc = await scrapingHandler.GetHtmlDocumentAsync($"{Url}/manga-list?page={page}");
                var links = doc
                    .QuerySelectorAll("div.thumb_attr.series-title > a")
                    .Select(x => x.As<IHtmlAnchorElement>().Href)
                    .ToList();
                doc.Close();
                return links;
            });

        var allUrlBatches = await Task.WhenAll(urlTasks);
        var allUrls = allUrlBatches.SelectMany(x => x).ToList();

        var mangas = new List<MangaObject>();
        await Parallel.ForEachAsync(allUrls, async (mangaUrl, _) => {
            try {
                var manga = await GetMangaAsync(mangaUrl);
                mangas.Add(manga);
            }
            catch (Exception ex) {
                logger.LogError("{}", ex);
            }
        });

        return mangas
            .GroupBy(x => x.Id)
            .Select(g => {
                if (g.Count() == 1)
                    return g.First();

                var lst = g.ToArray();
                return lst[0].Chapters.Count == lst[1].Chapters.Count &&
                       lst[0].Genres.Count > lst[1].Genres.Count ||
                       lst[0].Chapters.Count > lst[1].Chapters.Count
                    ? lst[0]
                    : lst[1];
            })
            .ToList();
    }

    public async Task<MangaObject> GetMangaAsync(string url) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(url);
        var name = document
            .QuerySelector("span.series-name > a")!
            .TextContent;

        var styleAttr = document.QuerySelector("div.img-in-ratio")!.GetAttribute("style") ?? string.Empty;
        var cover = CoverUrlRegex().Match(styleAttr).Groups[1].Value;

        var summary = document
            .QuerySelector("div.summary-content")!
            .TextContent
            .Clean();

        var authorText = GetInfoValue(document, "Author");
        IList<string> authors = string.IsNullOrEmpty(authorText) ? [] : [authorText];

        var genreText = GetInfoValue(document, "Genre");
        IList<string> genres = string.IsNullOrEmpty(genreText) ? [] : genreText.Slice(' ');

        IList<string> aliases = [
            GetInfoValue(document, "Other name"),
            GetInfoValue(document, "Doujinshi")
        ];

        var chapters = document
            .QuerySelectorAll("ul.list-chapters > a")
            .Select(x => {
                var chapterText = x.QuerySelector("div.chapter-name")!.TextContent;
                return new ChapterObject {
                    Title = chapterText.Clean(),
                    Number = ChapterNumberRegex().Match(chapterText).Value,
                    SourceUrl = x.As<IHtmlAnchorElement>().Href,
                    ReleasedOn = DateOnly.ParseExact(
                        x.QuerySelector("div.chapter-time")!.TextContent.Split('-')[1].Trim(),
                        "dd/MM/yyyy",
                        CultureInfo.InvariantCulture)
                };
            })
            .ToList();

        var coverPath = string.Empty;
        try {
            coverPath = await scrapingHandler.SaveCoverAsync(cover, Name.GetIdFromName(), name);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to download cover for {}", name);
        }

        var mangaObject = new MangaObject {
            Title = name,
            SourceId = Name.GetIdFromName(),
            SourceUrl = url,
            Summary = summary,
            Cover = cover,
            CoverPath = coverPath,
            Authors = authors,
            Genres = genres,
            Aliases = aliases,
            Chapters = chapters,
            UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        foreach (var provider in metadataProviders)
            try {
                var enrichment = await provider.FindMangaAsync(name);
                if (enrichment is null) continue;
                mangaObject = mangaObject.WithMetadata(enrichment);
                break;
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Metadata enrichment failed for {name} via {providerName}",
                    name,
                    provider.GetType().Name);
            }

        document.Close();
        return mangaObject;
    }

    public async Task<ChapterObject> FetchChapterAsync(ChapterObject chapter, string sourceId, string mangaId) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(chapter.SourceUrl);
        var element = document.All.First(x => x is { LocalName: "div", Id: "chapter-content" });
        var children = element.Children.ToList();

        var pages = new Dictionary<int, PageObject>();
        for (var i = 0; i < children.Count; i++)
            pages.Add(i, new PageObject(false, string.Empty, children[i].Attributes[1]!.Value));

        document.Close();
        return chapter with { Pages = pages };
    }

    private static string GetInfoValue(IDocument document, string infoName) {
        var infoElement = document
            .QuerySelectorAll("span.info-name")
            .FirstOrDefault(x => x.TextContent == $"{infoName}:");

        return infoElement
            ?.ParentElement
            ?.QuerySelector("span.info-value")
            ?.TextContent ?? string.Empty;
    }
}