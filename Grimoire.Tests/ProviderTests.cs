using Grimoire.Providers;
using Grimoire.Providers.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Tests;

[TestClass]
public class ProviderTests {
    [TestMethod]
    public async Task FetchMangasAsync() {
        var provider = Globals.Services.GetRequiredService<AsuraScansProvider>();
        Assert.IsNotNull(provider);

        var mangas = await provider.FetchMangasAsync();
        Assert.IsNotNull(mangas);
        Assert.IsTrue(mangas.Count > 0);
    }

    [TestMethod]
    public async Task FetchChaptersAsync() {
        var provider = Globals.Services.GetRequiredService<AsuraScansProvider>();
        Assert.IsNotNull(provider);

        var manga = new Manga {
            Url = "https://www.asurascans.com/manga/4569947261-the-knight-king-who-returned-with-a-god/"
        };
        
        var chapters = await provider.FetchChaptersAsync(manga);
        Assert.IsNotNull(chapters);
        Assert.IsTrue(chapters.Count > 0);
    }
}