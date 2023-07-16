using Grimoire.Commons.Interfaces;
using Microsoft.Extensions.Logging;

namespace Grimoire.Sources.Sources;

public sealed class PornwaClubSource : Manhwa18NetSource, IGrimoireSource {
    public new string Name
        => "Pornwa Club";

    public new string Url
        => "https://pornwa.club";

    public new string Icon
        => $"{Url}/favicon1.ico";

    public PornwaClubSource(ILogger<PornwaClubSource> logger) : base(logger) { }
}