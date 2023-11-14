using Grimoire.Handlers;
using Grimoire.Helpers;
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
    .AddLogging(x => {
        x.ClearProviders();
        x.AddConsole();
    })
    .AddSingleton<HttpHandler>()
    .AddGrimoireSources()
    .AddSingleton(new
            MongoClient(builder.Configuration["Mongo"])
        .GetDatabase(nameof(Grimoire)))
    .AddSingleton<CacheHandler>()
    .AddSingleton<DbHandler>()
    .AddHttpClient();

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