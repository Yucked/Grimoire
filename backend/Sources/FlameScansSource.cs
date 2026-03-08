using Grimoire.Handlers;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources;

public sealed partial class FlameScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<FlameScansSource> logger)
    : Commons.WordPressSource(scrapingHandler, metadataProviders, logger) {
    public override string Name
        => "Flame Scans";

    public override string Url
        => "https://flamecomics.com";

    public override string Icon
        => $"{Url}/favicon.ico";

    protected override string ListType
        => "series";

    protected override bool HandleRedirect
        => true;
}