using Grimoire.Commons;
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

await app.Services.GetRequiredService<ProxiesHandler>().VerifyProxies();

await app.RunAsync();