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
        var page = await scrapingHandler.RequestPageAsync("https://tcbscans.com/projects");
        var links = await page.QuerySelectorAllAsync("a.mb-3.text-white");
        var mangas = new List<MangaObject>();
        
        await Parallel.ForEachAsync(links, async (element, token) => {
            try {
                var href = await element.GetAttributeAsync("href");
                var manga = await GetMangaAsync($"https://tcbscans.com{href}");
                mangas.Add(manga);
            }
            catch (Exception exception) {
                logger.LogError("{}", exception);
            }
        });
        
        await page.CloseAsync();
        return mangas;
    }
    
    public async Task<MangaObject> GetMangaAsync(string url) {
        var page = await scrapingHandler.RequestPageAsync(url);
        var name = await page
            .QuerySelectorAsync("div.px-4 > h1")!
            .GetTextContentAsync();
        var cover = await page
            .QuerySelectorAsync("div.flex > img")!
            .GetAttributeAsync("src");
        var summary = await page
            .QuerySelectorAsync("p.leading-6")!
            .GetTextContentAsync();
        
        var chapters = new List<ChapterObject>();
        await Parallel.ForEachAsync(await page.QuerySelectorAllAsync("a.block.border"), async (element, _) => {
            var chapterHref = await element.GetAttributeAsync("href");
            
            var chapterNo = await (await element.QuerySelectorAsync("div.text-lg"))!
                .TextContentAsync();
            
            var chapterName = await (await element.QuerySelectorAsync("div.text-gray-500"))!
                .TextContentAsync();
            
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
            Type: MangaType.Manga);
        
        return mangaObject;
    }
    
    public async Task<ChapterObject> FetchChapterAsync(ChapterObject chapter) {
        var page = await scrapingHandler.RequestPageAsync(chapter.SourceUrl);
        var mangaPages = await page.QuerySelectorAllAsync("img.fixed-ratio-content");
        var sources = await mangaPages
            .Select(x => x.GetAttributeAsync("source"))
            .AwaitAsync();
        
        foreach (var (k, v) in sources
                     .Select((x, i) => new { x, i })
                     .ToDictionary(x => x.i, x => x.x)) {
            chapter.Pages.Add(k, new PageObject(false, default!, v!));
        }
        
        // TODO: Maybe store it in database directly?
        return chapter;
    }
}