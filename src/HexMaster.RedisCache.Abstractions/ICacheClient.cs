using StackExchange.Redis;

namespace HexMaster.RedisCache.Abstractions;

public interface ICacheClient
{
    Task SetAsAsync<T>(RedisKey key, T value, ushort minutes = 15);
    Task SetAsync(RedisKey key, RedisValue value, ushort minutes);

    Task<T?> GetAsAsync<T>(RedisKey key);
    Task<RedisValue> GetAsync(RedisKey key);

    Task<T> GetOrInitializeAsync<T>(Func<Task<T>> initializeFunction, RedisKey key, ushort timeoutInMinutes = 15);

    Task InvalidateAsync(RedisKey key);

    ISubscriber GetSubscriber();
    IDatabase GetDatabase();
}