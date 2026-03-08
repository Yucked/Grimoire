using Grimoire.Handlers;
using Grimoire.Sources.Commons;

namespace Grimoire.Sources;

public sealed partial class AsuraScansSource(
    ScrapingHandler scrapingHandler,
    IEnumerable<IMetadataProvider> metadataProviders,
    ILogger<AsuraScansSource> logger)
    : Commons.WordPressSource(scrapingHandler, metadataProviders, logger) {
    public override string Name
        => "Asura Scans";

    public override string Url
        => "https://asuratoon.com";

    public override string Icon
        => $"{Url}/wp-content/uploads/2021/03/Group_1.png";
}