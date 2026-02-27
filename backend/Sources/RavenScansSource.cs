using Grimoire.Handlers;
using Grimoire.Integrations;

namespace Grimoire.Sources;

public sealed partial class RavenScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<RavenScansSource> logger)
    : WordPressSource(scrapingHandler, metadataProviders, logger) {

    public override string Name => "Raven Scans";
    public override string Url => "https://ravenscans.com";
    public override string Icon => $"{Url}/favicon.ico";
}
