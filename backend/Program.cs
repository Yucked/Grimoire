using LiteDB;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", false, true);

builder.Services.AddControllers();
builder.Services.AddOutputCache();
builder.Services.AddMemoryCache(x =>
{
    x.CompactionPercentage = 80;
    x.ExpirationScanFrequency = TimeSpan.FromMinutes(30);
});
builder.Services.AddSingleton<ILiteDatabase>(new LiteDatabase("grimoire.db"));

var app = builder.Build();
app.MapControllers();
app.UseOutputCache(); // what does it do?
// app.UseResponseCaching();
// app.UseResponseCompression();

await app.RunAsync();