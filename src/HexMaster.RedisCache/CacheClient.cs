using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using StackExchange.Redis.Maintenance;

namespace HexMaster.RedisCache
{
    public class CacheClient : ICacheClient
    {

        private readonly IConnectionMultiplexer _cm;
        private readonly ILogger<CacheClient> _logger;
        private readonly IDatabase _database;

        public Task SetAsAsync<T>(RedisKey key, T value, ushort minutes = 15)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            return SetAsync(key, new RedisValue(jsonValue), minutes);
        }
        public async Task SetAsync(RedisKey key, RedisValue value, ushort minutes)
        {
            if (_cm.IsConnected)
            {
                await _database
                    .StringSetAsync(key, value, TimeSpan.FromMinutes(minutes))
                    .ConfigureAwait(false);
            }
        }

        public async Task<T?> GetAsAsync<T>(RedisKey key)
        {
            var redisValue = await GetAsync(key);
            ConvertToGenericType(redisValue, out T? typedObject);
            return typedObject;
        }
        public async Task<RedisValue> GetAsync(RedisKey key)
        {
            if (_cm.IsConnected)
            {
                return await _database.StringGetAsync(key).ConfigureAwait(false);
            }

            return RedisValue.Null;
        }

        public async Task<T> GetOrInitializeAsync<T>(Func<Task<T>> initializeFunction, RedisKey key, ushort timeoutInMinutes = 15)
        {
            try
            {
                if (_cm.IsConnected)
                {
                    var value = await _database.StringGetAsync(key);
                    if (ConvertToGenericType(value, out T? typedObject))
                    {
                        if (typedObject != null)
                        {
                            return typedObject;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Oops, apparently cache service is unavailable or connection was lost. falling back to the initialization function");
            }

            var initializedObject = await initializeFunction();
            var jsonValue = JsonConvert.SerializeObject(initializedObject);

            if (_cm.IsConnected)
            {
                await _database.StringSetAsync(key, jsonValue, TimeSpan.FromMinutes(timeoutInMinutes));
            }

            return initializedObject;
        }

        public async Task InvalidateAsync(RedisKey key)
        {
            if (_cm.IsConnected)
            {
                await _database
                    .KeyDeleteAsync(key)
                    .ConfigureAwait(false);
            }
        }

        public ISubscriber GetSubscriber() => _cm.GetSubscriber();
        public IDatabase GetDatabase() => _database;

        private static bool ConvertToGenericType<T>(RedisValue value, out T? typedObject)
        {
            typedObject = default;
            if (value.HasValue)
            {
                var deserializedObject = JsonConvert.DeserializeObject<T>(value.ToString());
                if (deserializedObject != null)
                {
                    typedObject = deserializedObject;
                    return true;
                }
            }

            return false;
        }

        #region [ Connection Multiplexer Event Handlers ]

        private void ErrorMessage(object? sender, RedisErrorEventArgs e)
        {
            _logger.LogError($"RedisCache.{_cm.ClientName}:{nameof(ErrorMessage)}: {{0}} from {{1}}", e.Message,
                e.EndPoint);
        }
        private void HashSlotMoved(object? sender, HashSlotMovedEventArgs e)
        {
            _logger.LogInformation($"RedisCache.{_cm.ClientName}:{nameof(HashSlotMoved)}: {{0}} moved from {{1}} to {{2}}",
                e.HashSlot, e.OldEndPoint, e.NewEndPoint);
        }
        private void ConnectionRestored(object? sender, ConnectionFailedEventArgs e)
        {
            _logger.LogInformation($"RedisCache.{_cm.ClientName}:{nameof(ConnectionRestored)}");
        }
        private void MaintenanceEvent(object? sender, ServerMaintenanceEvent e)
        {
            if (e is AzureMaintenanceEvent azureEvent)
            {
                _logger.LogInformation($"RedisCache.{_cm.ClientName}:{nameof(AzureMaintenanceEvent)}: {{0}}",
                    azureEvent.NotificationTypeString);
            }
        }
        private void ConnectionFailed(object? sender, ConnectionFailedEventArgs e)
        {
            _logger.LogError(e.Exception, $"RedisCache.{_cm.ClientName}:{nameof(ConnectionFailed)}: {{0}}", e.FailureType);
        }

        #endregion

        internal async Task DisposeAsync()
        {
            await _cm.CloseAsync().ConfigureAwait(false);
            _cm.Dispose();
        }

        internal CacheClient(IConnectionMultiplexer connectionMultiplexer,
            ILogger<CacheClient> logger)
        {
            _cm = connectionMultiplexer;
            _cm.ConnectionFailed += ConnectionFailed;
            _cm.ConnectionRestored += ConnectionRestored;
            _cm.HashSlotMoved += HashSlotMoved;
            _cm.ErrorMessage += ErrorMessage;

            // To catch maintenance event
            if (connectionMultiplexer is ConnectionMultiplexer instance)
            {
                instance.ServerMaintenanceEvent += MaintenanceEvent;
            }

            _database = _cm.GetDatabase();
            _logger = logger;

            _logger.LogInformation($"Redis Client Created:{_cm.ClientName}, state: {{0}}", _cm.GetStatus());
        }

    }
}
