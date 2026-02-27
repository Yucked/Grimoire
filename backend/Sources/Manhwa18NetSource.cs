using Grimoire.Handlers;
using Grimoire.Integrations;

namespace Grimoire.Sources;

public sealed partial class Manhwa18NetSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<Manhwa18NetSource> logger)
    : HanmaSource(scrapingHandler, metadataProviders, logger) {

    public override string Name
        => "Manhwa 18";

    public override string Url
        => "https://manhwa18.net";

    public override string Icon
        => $"{Url}/favicon1.ico";
}