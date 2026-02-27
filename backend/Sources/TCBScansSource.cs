using System.Text.RegularExpressions;
using Grimoire.Handlers;
using Grimoire.Integrations;
using Grimoire.Objects;

namespace Grimoire.Sources;

public sealed partial class TCBScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<TCBScansSource> logger) : IGrimoireSource {
    public string Name
    => "TCB Scans";

    public string Url
        => "https://tcbonepiecechapters.com";

    public string Icon
        => "https://tcbonepiecechapters.com/files/apple-touch-icon.png";

    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex ChapterNumberRegex();

    public async Task<IReadOnlyList<MangaObject>> GetMangasAsync() {
        var document = await scrapingHandler.GetHtmlDocumentAsync("https://tcbonepiecechapters.com/projects");
        var links = document.QuerySelectorAll("a.mb-3.text-white");
        var mangas = new List<MangaObject>();

        await Parallel.ForEachAsync(links, async (element, _) => {
            try {
                var href = element.GetAttribute("href");
                var manga = await GetMangaAsync($"https://tcbonepiecechapters.com{href}");
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
            .QuerySelector("div.px-4 > h1")!
            .TextContent;
        var cover = document
            .QuerySelector("div.flex > img")!
            .GetAttribute("src")!;
        var summary = document
            .QuerySelector("p.leading-6")!
            .TextContent;

        var chapters = new List<ChapterObject>();
        await Parallel.ForEachAsync(document.QuerySelectorAll("a.block.border"), async (element, _) => {
            var chapterHref = element.GetAttribute("href");
            var chapterNo = element.QuerySelector("div.text-lg")!.TextContent;
            var chapterName = element.QuerySelector("div.text-gray-500")!.TextContent;

            chapters.Add(new ChapterObject {
                Title = chapterName!,
                Number = ChapterNumberRegex().Match(chapterNo!).Value,
                SourceUrl = chapterHref!
            });
        });

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
            Chapters = chapters,
            UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        foreach (var provider in metadataProviders) {
            try {
                var enrichment = await provider.FindMangaAsync(name);
                if (enrichment is null) continue;
                mangaObject = mangaObject.WithMetadata(enrichment);
                break;
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Metadata enrichment failed for {} via {}", name, provider.GetType().Name);
            }
        }

        return mangaObject;
    }

    public async Task<ChapterObject> FetchChapterAsync(ChapterObject chapter, string sourceId, string mangaId) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(chapter.SourceUrl);
        var imageUrls = document
            .QuerySelectorAll("img.fixed-ratio-content")
            .Select(x => x.GetAttribute("source"))
            .Where(x => x is not null)
            .ToList();

        var pages = new Dictionary<int, PageObject>();
        for (var i = 0; i < imageUrls.Count; i++)
            pages.Add(i, new PageObject(false, string.Empty, imageUrls[i]!));

        return chapter with { Pages = pages };
    }
}
