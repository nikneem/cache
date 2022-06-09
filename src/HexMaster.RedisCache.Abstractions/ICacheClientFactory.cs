namespace HexMaster.RedisCache.Abstractions
{
    public interface ICacheClientFactory
    {
        ICacheClient CreateClient(string? name = null);
    }
}
