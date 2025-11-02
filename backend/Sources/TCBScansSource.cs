using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Grimoire.Handlers;
using Grimoire.Objects;

namespace Grimoire.Sources;

public sealed partial class TCBScansSource(
    ScrapingHandler scrapingHandler,
    ILogger<TCBScansSource> logger) : IGrimoireSource {
    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex ChapterNumberRegex();

    public async Task<IReadOnlyList<MangaObject>> GetMangasAsync() {
        var document = await scrapingHandler.GetHtmlDocumentAsync("https://tcbonepiecechapters.com/projects");
        var links = document.QuerySelectorAll("a.mb-3.text-white");
        var mangas = new List<MangaObject>();

        await Parallel.ForEachAsync(links, async (element, token) => {
            try {
                var href = element.GetAttribute("href");
                var manga = await GetMangaAsync($"https://tcbonepiecechapters.com{href}");
                mangas.Add(manga);
            }
            catch (Exception exception) {
                logger.LogError("{}", exception);
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
            .GetAttribute("src");
        var summary = document
            .QuerySelector("p.leading-6")!
            .TextContent;

        var chapters = new List<ChapterObject>();
        await Parallel.ForEachAsync(document.QuerySelectorAll("a.block.border"), async (element, _) => {
            var chapterHref = element.GetAttribute("href");
            var chapterNo = (element.QuerySelector("div.text-lg"))!.TextContent;
            var chapterName = (element.QuerySelector("div.text-gray-500"))!.TextContent;

            var chapterObject = new ChapterObject {
                Title = chapterName!,
                Number = $"{ChapterNumberRegex().Match(chapterNo!).Value:0.0}",
                SourceUrl = chapterHref!
            };

            chapters.Add(chapterObject);
        });

        var mangaObject = new MangaObject(
            Authors: default,
            Artists: default,
            Title: name,
            Aliases: default,
            Summary: summary,
            Genres: default,
            Status: MangaStatus.OnGoing,
            Cover: cover,
            LastChapterRead: -1,
            SourceUrl: url,
            Ratings: default,
            UpdatedAt: DateOnly.FromDateTime(DateTime.UtcNow),
            ReleasedOn: default,
            Chapters: chapters,
            Metadata: default,
            Type: MangaType.Manga,
            SourceId: nameof(TCBScansSource));

        return mangaObject;
    }

    public async Task<ChapterObject> FetchChapterAsync(ChapterObject chapter) {
        var document = await scrapingHandler.GetHtmlDocumentAsync(chapter.SourceUrl);
        var mangaPages = document.QuerySelectorAll("img.fixed-ratio-content");
        var sources = mangaPages
            .Select(x => x.GetAttribute("source"));

        foreach (var (k, v) in sources
                     .Select((x, i) => new { x, i })
                     .ToDictionary(x => x.i, x => x.x)) {
            chapter.Pages.Add(k, new PageObject(false, default!, v!));
        }

        // TODO: Maybe store it in database directly?
        return chapter;
    }
}