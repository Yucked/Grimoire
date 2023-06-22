using Grimoire.Sources;
using Grimoire.Sources.Interfaces;
using Grimoire.Sources.Models;
using Grimoire.Sources.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Tests;

[TestClass]
public sealed class SourceTests {
    private readonly Type _source
        = typeof(TCBScansSource);

    private static readonly Manga Manga = new() {
        Url = "https://flamescans.org/series/1686564121-forty-eight-hours-a-day/"
    };

    private static readonly Chapter Chapter = new() {
        Url = "https://tcbscans.com/chapters/43/bleach-chapter-686.5"
    };

    [TestMethod]
    public async Task FetchMangasAsync() {
        var source = Globals.Services.GetRequiredService(_source) as IGrimoireSource;
        Assert.IsNotNull(source);

        var mangas = await source.FetchMangasAsync();
        Assert.IsNotNull(mangas);
        Assert.IsTrue(mangas.Count > 0);
    }

    [TestMethod]
    public async Task FetchChaptersAsync() {
        var source = Globals.Services.GetRequiredService(_source) as IGrimoireSource;
        Assert.IsNotNull(source);

        var chapters = await source.FetchChaptersAsync(Manga);
        Assert.IsNotNull(chapters);
        Assert.IsTrue(chapters.Count > 0);
    }

    [TestMethod]
    public async Task FetchChapterAsync() {
        var source = Globals.Services.GetRequiredService(_source) as TCBScansSource;
        var chapter = await source!.FetchChapterAsync(Chapter);

        Assert.IsNotNull(chapter);
        Assert.IsNotNull(chapter.Pages);
        Assert.IsTrue(chapter.Pages.Count > 0);
    }

    [TestMethod]
    public async Task DownloadIconAsync() {
        var source = Globals.Services.GetRequiredService(_source) as IGrimoireSource;
        await Globals.Services.GetRequiredService<HttpClient>()
            .DownloadAsync(source!.Icon, Directory.GetCurrentDirectory());
    }
}