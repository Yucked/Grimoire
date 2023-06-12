using Grimoire.Providers;
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
}