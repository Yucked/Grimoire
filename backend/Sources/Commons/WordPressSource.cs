using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Grimoire.Handlers;
using Grimoire.Objects;

namespace Grimoire.Sources.Commons;

public abstract partial class WordPressSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger logger) : IGrimoireSource {
    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex ChapterNumberRegex();

    private static readonly char[] Separators = [',', '|'];

    protected virtual string ListType => "manga";
    protected virtual bool HandleRedirect => false;

    public abstract string Name { get; }
    public abstract string Url { get; }
    public abstract string Icon { get; }

    public async Task<IReadOnlyList<MangaObject>> GetMangasAsync() {
        var document = await scrapingHandler.GetHtmlDocumentAsync(
            $"{Url}/{ListType}/list-mode{(HandleRedirect ? "/" : "")}");
        var links = document
            .QuerySelectorAll("div.soralist > * a.series")
            .Select(x => x.As<IHtmlAnchorElement>().Href)
            .ToList();
        var mangas = new List<MangaObject>();

        await Parallel.ForEachAsync(links, async (link, _) => {
            try {
                var manga = await GetMangaAsync(link);
                mangas.Add(manga);
            }
            catch (Exception ex) {
                logger.LogError("{}", ex);
            }
        });

        document.Close();
        return mangas;
    }

    public async Task<MangaObject> GetMangaAsync(string url) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(url);
        var name = document
            .QuerySelector("h1.entry-title[itemprop='name']")!
            .TextContent
            .Clean();

        var cover = document
            .QuerySelector("img.wp-post-image")!
            .As<IHtmlImageElement>()
            .Source!;

        var chapters = document
            .GetElementById("chapterlist")!
            .FirstChild!
            .ChildNodes
            .Where(x => x is IHtmlListItemElement)
            .Select(x => {
                var li = x as IHtmlElement;
                var chapterText = li!
                    .GetElementsByClassName("chapternum")
                    .FirstOrDefault()
                    ?.TextContent ?? string.Empty;
                DateOnly releasedOn;
                try {
                    releasedOn = DateOnly.Parse(
                        li.GetElementsByClassName("chapterdate")
                            .FirstOrDefault()
                            ?.TextContent ?? string.Empty);
                }
                catch {
                    releasedOn = default;
                }

                return new ChapterObject {
                    Title = chapterText.Clean(),
                    Number = ChapterNumberRegex().Match(chapterText).Value,
                    SourceUrl = x.FindDescendant<IHtmlAnchorElement>()!.Href,
                    ReleasedOn = releasedOn
                };
            })
            .ToList();

        var summary = string.Empty;
        IList<string> aliases = [];
        IList<string> genres = [];
        IList<string> authors = [];

        try {
            summary = document
                .QuerySelector("*[itemprop='description']")!
                .Descendants()
                .Select(x => x.TextContent.Clean().Trim())
                .Join();

            aliases = document
                .GetElementsByClassName("alternative")
                .FirstOrDefault()
                ?.TextContent
                .Clean()
                .Slice(Separators) ?? [];

            genres = document
                .QuerySelector("div.wd-full > span.mgen")
                ?.TextContent
                .Slice(' ') ?? [];

            var authorText = document
                .QuerySelectorAll("div.tsinfo > div.imptdt")
                .FirstOrDefault(x => x.TextContent.Clean().Trim().StartsWith("Author", StringComparison.Ordinal))
                ?.TextContent
                .Slice(' ')[1..]
                .Join()
                .Clean()
                .Trim();

            if (!string.IsNullOrEmpty(authorText))
                authors = [authorText];
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to scrape optional fields for {}", name);
        }

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
            Aliases = aliases,
            Genres = genres,
            Authors = authors,
            Chapters = chapters,
            UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        foreach (var provider in metadataProviders)
            try {
                var enrichment = await provider.FindMangaAsync(name);
                if (enrichment is null) {
                    continue;
                }

                mangaObject = mangaObject.WithMetadata(enrichment);
                break;
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Metadata enrichment failed for {name} via {providerName}",
                    name,
                    provider.GetType().Name);
            }

        return mangaObject;
    }

    public async Task<ChapterObject> FetchChapterAsync(ChapterObject chapter, string sourceId, string mangaId) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(chapter.SourceUrl);
        var chapterId = document
            .Head!
            .Descendants<IHtmlLinkElement>()
            .First(x => x is { Type: "application/json", Relation: "alternate" })
            .Href!
            .Split('/')[^1];

        var htmlImages = document
            .GetElementById("readerarea")!
            .Descendants<IHtmlImageElement>()
            .Select(x => x.Source)
            .Where(x => x is not null)
            .ToList();

        using var jsonDocument = await scrapingHandler.GetJsonDocumentAsync(
            $"{Url}/wp-json/wp/v2/posts/{chapterId}");
        var html = jsonDocument.RootElement
            .GetProperty("content")
            .GetProperty("rendered")
            .GetString()!;
        var jsonPageDoc = await scrapingHandler.ParseHtmlAsync(html);
        var jsonImages = jsonPageDoc
            .Descendants<IHtmlImageElement>()
            .Select(x => x.Source)
            .Where(x => x is not null)
            .ToList();

        var imageUrls = jsonImages.Count > htmlImages.Count ? jsonImages : htmlImages;
        var pages = new Dictionary<int, PageObject>();
        for (var i = 0; i < imageUrls.Count; i++)
            pages.Add(i, new PageObject(false, string.Empty, imageUrls[i]!));

        document.Close();
        return chapter with { Pages = pages };
    }
}