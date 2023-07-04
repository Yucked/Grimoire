namespace Grimoire.Web.Handlers.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record UserLibrary {
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string Id
        => nameof(Grimoire);

    public List<string> Favourites { get; set; }
}