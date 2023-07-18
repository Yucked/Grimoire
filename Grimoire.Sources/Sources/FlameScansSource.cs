using Grimoire.Commons;
using Grimoire.Commons.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public class FlameScansSource : ArenaScansSource, IGrimoireSource {
    public new string Name
        => "Flame Scans";

    public new string Url
        => "https://flamescans.org";

    public new string Icon
        => $"{Url}/favicon.ico";

    private readonly HtmlParser _htmlParser;
    private readonly ILogger<FlameScansSource> _logger;

    public FlameScansSource(ILogger<FlameScansSource> logger, HtmlParser htmlParser)
        : base(logger, htmlParser) {
        _logger = logger;
        _htmlParser = htmlParser;
    }
}