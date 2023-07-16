using Grimoire.Commons.Parsing;
using Grimoire.Commons.Proxy;
using Grimoire.Web;
using Grimoire.Web.Handlers;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", false, true);

builder
    .Services
    .AddRazorPages()
    .Services
    .AddServerSideBlazor()
    .Services
    .AddOutputCache()
    .AddResponseCaching()
    .AddResponseCompression()
    .AddMemoryCache()
    .AddHttpClient()
    .AddLogging(x => {
        x.ClearProviders();
        x.AddConsole();
    })
    .AddSingleton(new ParserOptions(
        builder.Configuration.GetValue<int>("MaxDelay"),
        builder.Configuration.GetValue<int>("MaxRetries"),
        builder.Configuration.GetSection("Proxies").Get<string[]>(),
        builder.Configuration.GetSection("UserAgents").Get<string[]>()
    ))
    .AddSingleton<ProxiesHandler>()
    .AddSingleton<HtmlParser>()
    .AddGrimoireSources()
    .AddSingleton(new
            MongoClient(builder.Configuration["Mongo"])
        .GetDatabase(nameof(Grimoire)))
    .AddSingleton<CacheHandler>()
    .AddSingleton<DbHandler>();

var app = builder.Build();
app.UseHttpsRedirection()
    .UseStaticFiles()
    .UseCustomStaticFiles(app)
    .UseRouting()
    .UseResponseCaching()
    .UseResponseCompression()
    .UseOutputCache();

app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();