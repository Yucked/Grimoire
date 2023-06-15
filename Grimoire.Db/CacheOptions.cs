namespace Grimoire.Db; 

public record CacheOptions {
    public string RedisHost { get; init; }
    public string RedisPort { get; init; }
    public string SaveTo { get; init; }
}