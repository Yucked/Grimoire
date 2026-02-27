using Grimoire.Handlers;
using Grimoire.Integrations;

namespace Grimoire.Sources;

public sealed partial class PornwaClubSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<PornwaClubSource> logger)
    : HanmaSource(scrapingHandler, metadataProviders, logger) {

    public override string Name
        => "Pornwa Club";

    public override string Url
        => "https://pornwa.club";

    public override string Icon
        => $"{Url}/favicon.ico";
}