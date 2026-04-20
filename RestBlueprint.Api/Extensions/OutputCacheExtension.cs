namespace RestBlueprint.Api.Extensions;

/// <summary>
/// Registers output caching, using Redis when configured and falling back to in-memory storage.
/// </summary>
/// <remarks>
/// <b>Redis setup:</b> Set <c>ConnectionStrings:redis</c> in <c>appsettings.json</c> or an environment
/// variable. Leave empty to use the built-in in-memory store (default for local development).
///
/// <b>How to use caching in endpoints:</b>
/// <list type="bullet">
///   <item><c>.CacheOutput(p => p.Tag("resource-name"))</c> — cache and tag a GET response.</item>
///   <item><c>await cache.EvictByTagAsync("resource-name", ct)</c> — evict on POST/PUT/PATCH/DELETE.</item>
///   <item>Inject <see cref="IOutputCacheStore"/> into the endpoint handler to evict.</item>
/// </list>
///
/// See <c>ArticleEndpoints.cs</c> for a worked example of tag-based eviction.
/// </remarks>
internal static class OutputCacheExtension
{
    /// <summary>
    /// Adds output caching services.  Redis is used if the connection string is configured;
    /// otherwise the default in-memory store is used.
    /// </summary>
    public static IServiceCollection AddOutputCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? redisConnectionString = configuration.GetConnectionString("redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // Redis output cache — shared across multiple API instances.
            services.AddStackExchangeRedisOutputCache(options =>
            {
                options.Configuration = redisConnectionString;
                // TEMPLATE NOTE: Change this instance name to match your project.
                options.InstanceName = "RestBlueprintAPI";
            });
        }

        // AddOutputCache registers the middleware and IOutputCacheStore regardless of Redis.
        services.AddOutputCache();

        return services;
    }
}
