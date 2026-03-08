using Grimoire.Handlers;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources;

public sealed class RavenScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<RavenScansSource> logger)
    : WordPressSource(scrapingHandler, metadataProviders, logger) {
    public override string Name
        => "Raven Scans";

    public override string Url
        => "https://ravenscans.com";

    public override string Icon
        => "https://i3.wp.com/ravenscans.org/wp-content/uploads/2025/05/cropped-favicon-192x192.png";
}