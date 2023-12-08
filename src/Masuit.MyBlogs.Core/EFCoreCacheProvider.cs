﻿using EFCoreSecondLevelCacheInterceptor;
using FreeRedis;
using Masuit.MyBlogs.Core.Common;

namespace Masuit.MyBlogs.Core;

public class EFCoreCacheProvider(IRedisClient redisClient, ILogger<EFCoreCacheProvider> cacheLogger, IEFDebugLogger logger) : IEFCacheServiceProvider
{
    /// <summary>
    ///     Adds a new item to the cache.
    /// </summary>
    /// <param name="cacheKey">key</param>
    /// <param name="value">value</param>
    /// <param name="cachePolicy">Defines the expiration mode of the cache item.</param>
    public void InsertValue(EFCacheKey cacheKey, EFCachedData value, EFCachePolicy cachePolicy)
    {
        if (cacheKey is null)
        {
            throw new ArgumentNullException(nameof(cacheKey));
        }

        value ??= new EFCachedData
        {
            IsNull = true
        };

        var keyHash = cacheKey.KeyHash;

        foreach (var rootCacheKey in cacheKey.CacheDependencies)
        {
            if (string.IsNullOrWhiteSpace(rootCacheKey))
            {
                continue;
            }
            redisClient.SAdd(rootCacheKey, keyHash);
            redisClient.Expire(rootCacheKey, 3600);
        }

        if (cachePolicy == null)
        {
            redisClient.Set(keyHash, value, 300);
        }
        else
        {
            redisClient.AddOrUpdate(keyHash, value, value, cachePolicy.CacheTimeout, cachePolicy.CacheExpirationMode == CacheExpirationMode.Sliding);
        }
    }

    /// <summary>Removes the cached entries added by this library.</summary>
    public void ClearAllCachedEntries()
    {
        redisClient.Del("EFCache:*");
    }

    /// <summary>
    ///     Gets a cached entry by key.
    /// </summary>
    /// <param name="cacheKey">key to find</param>
    /// <returns>cached value</returns>
    /// <param name="cachePolicy">Defines the expiration mode of the cache item.</param>
    public EFCachedData GetValue(EFCacheKey cacheKey, EFCachePolicy cachePolicy)
    {
        if (cacheKey is null)
        {
            throw new ArgumentNullException(nameof(cacheKey));
        }

        return redisClient.Get<EFCachedData>(cacheKey.KeyHash);
    }

    /// <summary>
    ///     Invalidates all of the cache entries which are dependent on any of the specified root keys.
    /// </summary>
    /// <param name="cacheKey">Stores information of the computed key of the input LINQ query.</param>
    public void InvalidateCacheDependencies(EFCacheKey cacheKey)
    {
        if (cacheKey is null)
        {
            throw new ArgumentNullException(nameof(cacheKey));
        }

        foreach (var rootCacheKey in cacheKey.CacheDependencies)
        {
            if (string.IsNullOrWhiteSpace(rootCacheKey))
            {
                continue;
            }

            var cachedValue = redisClient.Get<EFCachedData>(cacheKey.KeyHash);
            var dependencyKeys = redisClient.SMembers(rootCacheKey);
            if (dependencyKeys.IsNullOrEmpty() && cachedValue is not null)
            {
                if (logger.IsLoggerEnabled)
                {
                    cacheLogger.LogDebug(CacheableEventId.QueryResultInvalidated, "Invalidated all of the cache entries due to early expiration of a root cache key[{RootCacheKey}].", rootCacheKey);
                }

                redisClient.Del(rootCacheKey);
                return;
            }
            redisClient.Del(rootCacheKey);
        }
    }
}
