using Grimoire.Sources.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Grimoire.Sources.Miscellaneous;

public static partial class Misc {
    public static IServiceCollection AddGrimoireSources(this IServiceCollection collection) {
        var sources = typeof(IGrimoireSource).Assembly
            .GetTypes()
            .Where(x => typeof(IGrimoireSource).IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract);

        foreach (var source in sources) {
            collection.AddSingleton(source);
        }

        return collection;
    }

    public static IEnumerable<IGrimoireSource> GetGrimoireSources(this IServiceProvider provider) {
        var sources = typeof(IGrimoireSource).Assembly
            .GetTypes()
            .Where(x => typeof(IGrimoireSource).IsAssignableFrom(x)
                        && !x.IsInterface
                        && !x.IsAbstract);

        return sources
            .Select(provider.GetRequiredService)
            .OfType<IGrimoireSource>();
    }
}