using Grimoire.Commons.Interfaces;
using Grimoire.Providers;

namespace Grimoire.Web;

public static class Extensions {
    public static IServiceCollection AddGrimoireProviders(this IServiceCollection collection) {
        var providers = typeof(Globals).Assembly
            .GetTypes()
            .Where(x => typeof(IGrimoireProvider).IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract);

        foreach (var provider in providers) {
            collection.AddSingleton(provider);
        }

        return collection;
    }

    public static IEnumerable<IGrimoireProvider> GetProviders(this IServiceProvider provider) {
        var providers = typeof(Globals).Assembly
            .GetTypes()
            .Where(x => typeof(IGrimoireProvider).IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract);

        return providers
            .Select(provider.GetRequiredService)
            .OfType<IGrimoireProvider>();
    }
}