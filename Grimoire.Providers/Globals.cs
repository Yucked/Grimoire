using AngleSharp;

namespace Grimoire.Providers; 

public static class Globals {
    public static IBrowsingContext Context
        = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
}