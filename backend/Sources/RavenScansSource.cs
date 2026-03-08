using Grimoire.Handlers;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources;

public sealed partial class RavenScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<RavenScansSource> logger)
    : Commons.WordPressSource(scrapingHandler, metadataProviders, logger) {
    public override string Name => "Raven Scans";
    public override string Url => "https://ravenscans.com";
    public override string Icon => $"{Url}/favicon.ico";
}