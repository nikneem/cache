# HexMaster Cache Package

This is a wrapper when using Azure Cache for Redis. It implements the Cache-Aside pattern and allows you to quickly and easily access your cache instance. The package manages connections and connection issues for you.

## Configuration

When using this package, you need to add two configuration values (or a configuration object):

```
Cache.Endpoint
Cache.Secret
```

Or a JSON object, for example when you use `appsettings.json` in your ASP.NET project, add the following object:

```json
"Cache": {
    "Endpoint": "https://url-to.your.cache:instance",
    "Secret": "The secret to connect to your cache instance"
}
```

## Usage

From your startup instance, simply add the cache service to your services collection by calling `services.AddHexMasterCache();`. Then use Constructor injection to intject the `ICacheClientFactory` object. This interface can return a `ICacheClient` allowing you access your cache by calling the `GetClient()` fuction.
