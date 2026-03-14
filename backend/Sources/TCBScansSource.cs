using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using Grimoire.Handlers;
using Grimoire.Objects;
using Grimoire.Sources.Commons;

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
        await Parallel.ForEachAsync(document.QuerySelectorAll("a.block.border"), (element, _) => {
            var chapterHref = element.GetAttribute("href");
            var chapterNo = element.QuerySelector("div.text-lg")!.TextContent;
            var chapterName = element.QuerySelector("div.text-gray-500")!.TextContent;

            chapters.Add(new ChapterObject {
                Title = chapterName,
                Number = ChapterNumberRegex().Match(chapterNo).Value,
                SourceUrl = chapterHref!
            });
            return ValueTask.CompletedTask;
        });

        var coverPath = await scrapingHandler.SaveCoverSafeAsync(cover, Name.GetIdFromName(), name, logger);
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

        return await mangaObject.EnrichWithMetadataAsync(name, metadataProviders, logger);
    }

    public async Task<ChapterObject> FetchChapterAsync(ChapterObject chapter, string sourceId, string mangaId) {
        var chapterUrl = chapter.SourceUrl.StartsWith("http") ? chapter.SourceUrl : $"{Url}{chapter.SourceUrl}";
        var document = await scrapingHandler.GetHtmlDocumentAsync(chapterUrl);
        var imageUrls = document
            .QuerySelectorAll("img.fixed-ratio-content")
            .Select(x => x.As<IHtmlImageElement>().Source)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return chapter with { Pages = imageUrls! };
    }
}