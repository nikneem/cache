using System.Runtime.ExceptionServices;
using HexMaster.RedisCache.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HexMaster.RedisCache
{
    public class CacheClientRegistration
    {
        public string Name { get; }
        
        private readonly object _cacheLock = new();
        private CacheClient? _cachedClient;
        private ExceptionDispatchInfo? _cachedException;

        public CacheClient GetClient(CacheConfiguration config, ILogger<CacheClient> logger)
        {
            _cachedException?.Throw();

            if (_cachedClient != null)
            {
                return _cachedClient;
            }

            lock (_cacheLock)
            {
                _cachedException?.Throw();

                if (_cachedClient != null)
                {
                    return _cachedClient;
                }

                try
                {
                    var cm = ConnectionMultiplexer.Connect(GetDefaultConfigurationOptions(config));
                    _cachedClient = new CacheClient(cm, logger);
                }
                catch (Exception e)
                {
                    _cachedException = ExceptionDispatchInfo.Capture(e);
                    throw;
                }

                return _cachedClient;
            }
        }

        private  ConfigurationOptions GetDefaultConfigurationOptions(CacheConfiguration config) => new()
        {
            ClientName = Name,
            Password = config.Secret,
            EndPoints = { config.Endpoint },
            AllowAdmin = false,
            AbortOnConnectFail = false,
            Ssl = true,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            ReconnectRetryPolicy = new ExponentialRetry(30, 500),
            SyncTimeout = 500,
            AsyncTimeout = 500,
            ConnectTimeout = 2000,
            DefaultVersion = new Version("6.0.14")
        };

        internal CacheClientRegistration(string name)
        {
            Name = name;
        }
    }
}
