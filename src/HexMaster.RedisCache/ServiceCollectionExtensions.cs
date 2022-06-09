using System.Text.RegularExpressions;
using HexMaster.RedisCache.Abstractions;
using HexMaster.RedisCache.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HexMaster.RedisCache;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddHexMasterCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CacheConfiguration>(configuration.GetSection(CacheConfiguration.SectionName));
        services.AddHexMasterCacheClient(Constants.DefaultCacheClientName);
        services.TryAddSingleton<ICacheClientFactory, CacheClientFactory>();
        return services;
    }

    public static IServiceCollection AddHexMasterCacheClient(this IServiceCollection collection, string clientName)
    {
        ValidateClientName(clientName);
        collection.TryAddSingleton(sp => new CacheClientRegistration(clientName));
        return collection;
    }

    private static void ValidateClientName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name),
                "A cache client must have a name to prevent conflicts between clients");
        }

        if (!Regex.IsMatch(name, Constants.ValidClientNameRegularExpression))
        {
            throw new ArgumentException(
                "The name of a cache client can only contain alphabetical characters (uppercase and lowercase)");
        }
    }
}