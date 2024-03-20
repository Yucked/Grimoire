var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("config.json", false, true);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
// app.UseResponseCaching();
// app.UseResponseCompression();

await app.RunAsync();