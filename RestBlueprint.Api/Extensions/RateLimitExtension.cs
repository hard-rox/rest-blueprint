using System.Threading.RateLimiting;

namespace RestBlueprint.Api.Extensions;

/// <summary>
/// Registers a global, per-IP-address fixed-window rate limiter.
/// </summary>
/// <remarks>
/// <b>Default policy:</b> 100 requests per minute per client IP.  Excess requests receive
/// <c>HTTP 429 Too Many Requests</c> immediately (no queue).
///
/// <b>How to customise:</b>
/// <list type="bullet">
///   <item>Change <c>PermitLimit</c> / <c>Window</c> to tighten or relax the global limit.</item>
///   <item>Add a named limiter with <c>options.AddFixedWindowLimiter("premium", ...)</c> and apply it
///         per-endpoint with <c>.RequireRateLimiting("premium")</c>.</item>
///   <item>Switch to <c>SlidingWindowLimiter</c> or <c>TokenBucketLimiter</c> for different
///         traffic shapes.</item>
/// </list>
/// </remarks>
internal static class RateLimitExtension
{
    /// <summary>Adds a global per-IP fixed-window rate limiter (100 req/min).</summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Partition by remote IP so limits are per-client, not global.
                string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,                           // requests allowed per window
                    Window = TimeSpan.FromMinutes(1),            // rolling window duration
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0                               // reject immediately — no queuing
                });
            });

            // Return 429 instead of the default 503 when the limit is exceeded.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
