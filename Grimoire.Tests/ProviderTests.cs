using Grimoire.Sources;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Grimoire.Sources.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Tests;

[TestClass]
public sealed class ProviderTests {
    private readonly Type _provider
        = typeof(TCBScansSource);

    private static readonly Manga Manga = new() {
        Url = "https://flamescans.org/series/1686564121-forty-eight-hours-a-day/"
    };

    private static readonly Chapter Chapter = new() {
        Url = "https://tcbscans.com/chapters/43/bleach-chapter-686.5"
    };

    [TestMethod]
    public async Task FetchMangasAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as IGrimoireSource;
        Assert.IsNotNull(provider);

        var mangas = await provider.FetchMangasAsync();
        Assert.IsNotNull(mangas);
        Assert.IsTrue(mangas.Count > 0);
    }

    [TestMethod]
    public async Task FetchChaptersAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as IGrimoireSource;
        Assert.IsNotNull(provider);

        var chapters = await provider.FetchChaptersAsync(Manga);
        Assert.IsNotNull(chapters);
        Assert.IsTrue(chapters.Count > 0);
    }

    [TestMethod]
    public async Task FetchChapterAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as TCBScansSource;
        var chapter = await provider!.FetchChapterAsync(Chapter);

        Assert.IsNotNull(chapter);
        Assert.IsNotNull(chapter.Pages);
        Assert.IsTrue(chapter.Pages.Count > 0);
    }

    [TestMethod]
    public async Task DownloadIconAsync() {
        var provider = Globals.Services.GetRequiredService(_provider) as IGrimoireSource;
        await Globals.Services.GetRequiredService<HttpClient>()
            .DownloadAsync(provider!.Icon, Directory.GetCurrentDirectory());
    }
}