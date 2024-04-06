using Grimoire.Handlers;
using Grimoire.Objects;

namespace Grimoire.Sources;

public sealed class TCBScansSource(ScrapingHandler scrapingHandler) : IGrimoireSource {
    public async Task<IReadOnlyList<MangaObject>> GetMangasAsync() {
        var page = await scrapingHandler.RequestPageAsync("https://tcbscans.com/projects");
        var links = await page.QuerySelectorAllAsync("a.mb-3.text-white");

        var results = links
            .Select(async element => {
                var href = await element.GetAttributeAsync("href");
                return await GetMangaAsync($"https://tcbscans.com{href}");
            })
            .AwaitAsync();

        await page.CloseAsync();
        return await results;
    }

    public async Task<MangaObject> GetMangaAsync(string url) {
        var page = await scrapingHandler.RequestPageAsync(url);
        var name = await page
            .QuerySelectorAsync("div.px-4 > h1")!
            .GetTextContentAsync();
        var cover = await page
            .QuerySelectorAsync("div.flex > img")!
            .GetAttributeAsync("src");

        new MangaObject()
        return default;
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