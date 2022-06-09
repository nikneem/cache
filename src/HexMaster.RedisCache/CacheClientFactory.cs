using HexMaster.RedisCache.Abstractions;
using HexMaster.RedisCache.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HexMaster.RedisCache
{
    internal class CacheClientFactory : ICacheClientFactory
    {
        private readonly IOptions<CacheConfiguration> _cacheConfigurationOptions;
        private readonly ILogger<CacheClientFactory> _logger;
        private readonly ILogger<CacheClient> _clientLogger;
        private readonly Dictionary<string, CacheClientRegistration> _registrations;


        private void SetMinThreads()
        {
            var expectedMinWorkerThreads = _registrations.Count + 2;

            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
            if (minWorkerThreads < expectedMinWorkerThreads)
            {
                _logger.LogInformation($"Increasing minimum worker threads from {minWorkerThreads} to {expectedMinWorkerThreads}");
                minWorkerThreads = expectedMinWorkerThreads;
            }
            ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);
        }

        public ICacheClient CreateClient(string? name = null)
        {
            var clientName = name ?? Constants.DefaultCacheClientName;
            if (!_registrations.TryGetValue(clientName, out CacheClientRegistration? registration))
            {
                throw new InvalidOperationException($"Unable to find client registration with name '{clientName}'.");
            }

            return registration.GetClient(_cacheConfigurationOptions.Value, _clientLogger);
        }

        internal CacheClientFactory(
            IOptions<CacheConfiguration> cacheConfigurationOptions,
            IEnumerable<CacheClientRegistration> clientRegistrations,
            ILogger<CacheClientFactory> logger,
            ILogger<CacheClient> clientLogger
            )
        {
            _cacheConfigurationOptions = cacheConfigurationOptions;
            _logger = logger;
            _clientLogger = clientLogger;
            _registrations = new Dictionary<string, CacheClientRegistration>();
            foreach (var registration in clientRegistrations.Distinct())
            {
                _registrations[registration.Name] = registration;
            }

            SetMinThreads();
        }
    }
}
