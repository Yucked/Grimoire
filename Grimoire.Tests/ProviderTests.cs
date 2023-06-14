using Grimoire.Providers;
using Grimoire.Providers.Interfaces;
using Grimoire.Providers.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Tests;

[TestClass]
public sealed class ProviderTests {
    private readonly Type _provider
        = typeof(FlameScansProvider);

    private static Manga Manga => new() {
        Url = "https://flamescans.org/series/1686564121-forty-eight-hours-a-day/"
    };

    private static MangaChapter Chapter = new() {
        Url = "https://tcbscans.com/chapters/43/bleach-chapter-686.5"
    };

    [TestMethod]
    public async Task FetchMangasAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as IGrimoireProvider;
        Assert.IsNotNull(provider);

        var mangas = await provider.FetchMangasAsync();
        Assert.IsNotNull(mangas);
        Assert.IsTrue(mangas.Count > 0);
    }

    [TestMethod]
    public async Task FetchChaptersAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as IGrimoireProvider;
        Assert.IsNotNull(provider);

        var chapters = await provider.FetchChaptersAsync(Manga);
        Assert.IsNotNull(chapters);
        Assert.IsTrue(chapters.Count > 0);
    }

    [TestMethod]
    public async Task GetChapterAsync() {
        var provider = Globals.Services.GetRequiredService<TCBScansProvider>();
        var chapter = await provider.GetChapterAsync(Chapter);

        Assert.IsNotNull(chapter);
        Assert.IsNotNull(chapter.Pages);
        Assert.IsTrue(chapter.Pages.Count > 0);
    }
}