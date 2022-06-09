namespace HexMaster.RedisCache.Configuration;

public class CacheConfiguration
{
    public const string SectionName = "Cache";

    public string? Endpoint { get; set; }
    public string? Secret { get; set; }
}