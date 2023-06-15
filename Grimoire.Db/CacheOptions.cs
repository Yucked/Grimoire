namespace Grimoire.Db; 

public struct CacheOptions {
    public string RedisHost { get; init; }
    public string RedisPort { get; init; }
    public string SaveTo { get; init; }
}