using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HexMaster.RedisCache
{
    public class CacheClient : ICacheClient
    {
        internal CacheClient(IConnectionMultiplexer connectionMultiplexer, ILogger<CacheClient> logger)
        {

        }
    }
}
