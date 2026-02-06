// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OpenTweak.Services;

/// <summary>
/// Simple in-memory cache for PCGW results with TTL expiration.
/// Reduces API calls and improves UI responsiveness.
/// </summary>
public class PCGWCache
{
    private readonly ConcurrentDictionary<string, CachedResult> _cache = new();
    private readonly TimeSpan _ttl;
    private readonly ILogger<PCGWCache> _logger;

    /// <summary>
    /// Creates a new cache with the specified TTL.
    /// </summary>
    /// <param name="ttl">Time-to-live for cached entries. Defaults to 30 minutes.</param>
    /// <param name="logger">Logger for cache operations.</param>
    public PCGWCache(ILogger<PCGWCache> logger, TimeSpan? ttl = null)
    {
        _logger = logger;
        _ttl = ttl ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Gets a cached result or fetches it using the provided function.
    /// </summary>
    public async Task<PCGWGameInfo?> GetOrFetchAsync(string gameTitle, Func<Task<PCGWGameInfo?>> fetcher)
    {
        var key = NormalizeKey(gameTitle);

        // Check cache first
        if (_cache.TryGetValue(key, out var cached) && !cached.IsExpired)
        {
            _logger.LogDebug("Cache hit for {GameTitle}", gameTitle);
            return cached.Value;
        }

        // Fetch and cache
        _logger.LogDebug("Cache miss for {GameTitle}, fetching from PCGW", gameTitle);
        var result = await fetcher();

        var entry = new CachedResult(result, _ttl);
        _cache.AddOrUpdate(key, entry, (_, _) => entry);

        return result;
    }

    /// <summary>
    /// Invalidates the cache entry for a specific game.
    /// </summary>
    public void Invalidate(string gameTitle)
    {
        var key = NormalizeKey(gameTitle);
        if (_cache.TryRemove(key, out _))
        {
            _logger.LogDebug("Invalidated cache for {GameTitle}", gameTitle);
        }
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void InvalidateAll()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation("Cleared {Count} cached PCGW entries", count);
    }

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    public int Count => _cache.Count;

    private static string NormalizeKey(string gameTitle)
    {
        return gameTitle.Trim().ToLowerInvariant();
    }

    private class CachedResult
    {
        public PCGWGameInfo? Value { get; }
        public DateTimeOffset ExpiresAt { get; }

        public CachedResult(PCGWGameInfo? value, TimeSpan ttl)
        {
            Value = value;
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl);
        }

        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
