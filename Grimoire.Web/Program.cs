using Grimoire.Commons;
using Grimoire.Web;
using Grimoire.Web.Handlers;
using MongoDB.Driver;
using Torch;

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
    .AddSingleton<HtmlParser>()
    .AddGrimoireSources()
    .AddSingleton(new
            MongoClient(builder.Configuration["Mongo"])
        .GetDatabase(nameof(Grimoire)))
    .AddSingleton<CacheHandler>()
    .AddSingleton<DbHandler>()
    .AddSingleton<TorchClient>()
    .AddSingleton(x => x.GetRequiredService<TorchClient>().HttpClient);

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

var torchClient = app.Services.GetRequiredService<TorchClient>();
app.Lifetime.ApplicationStopping.Register(() => torchClient.TerminateAsync().GetAwaiter().GetResult());
await torchClient.InitializeAsync();

await app.RunAsync();