using System.Text;
using System.Text.Json;

namespace RestBlueprint.Api.Endpoints;

// =============================================================================
// TEMPLATE NOTE — Server-Sent Events (SSE)
// =============================================================================
// SSE is a standard HTTP streaming mechanism for pushing real-time events from
// server to client over a plain HTTP/1.1 connection.  The client opens a long-
// lived GET request and the server writes "data: ...\n\n" lines at will.
//
// THIS IMPLEMENTATION streams heartbeat events every 5 seconds to showcase the
// wire format.  In a real application you would:
//   1. Inject a System.Threading.Channels.Channel<ServerEvent> or an event bus.
//   2. Replace the heartbeat loop with an await foreach over the channel reader.
//   3. Broadcast domain events (e.g. ArticlePublished) onto the channel from
//      other parts of the application.
//
// IMPORTANT: SSE does NOT go through output caching.  The endpoint must be
// excluded from rate-limiting or given a generous per-client limit in production.
//
// Wire format (each event block):
//   id: <sequential id>
//   event: <event type>
//   data: <JSON payload>
//   \n
// =============================================================================

/// <summary>
/// Server-Sent Events endpoint demonstrating real-time streaming from server to client.
/// </summary>
public sealed class SseEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        // -----------------------------------------------------------------
        // GET /events/stream  — SSE heartbeat stream
        // -----------------------------------------------------------------
        routeBuilder.MapGet("/events/stream",
                async (HttpContext httpContext, CancellationToken ct) =>
                {
                    // SSE requires these headers.  Do not use compression on an SSE stream.
                    httpContext.Response.Headers["Content-Type"] = "text/event-stream";
                    httpContext.Response.Headers["Cache-Control"] = "no-cache";
                    httpContext.Response.Headers["Connection"] = "keep-alive";
                    httpContext.Response.Headers["X-Accel-Buffering"] = "no"; // Disable nginx buffering

                    await httpContext.Response.Body.FlushAsync(ct);

                    int eventId = 0;

                    // Keep streaming until the client disconnects or the server shuts down.
                    while (!ct.IsCancellationRequested)
                    {
                        eventId++;

                        // Build the SSE event payload.
                        var payload = new
                        {
                            timestamp = DateTime.UtcNow,
                            eventType = "heartbeat",
                            message = "Server is alive",
                            eventId
                        };

                        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                        });

                        // Write the SSE event block.
                        string sseBlock =
                            $"id: {eventId}\n" +
                            $"event: heartbeat\n" +
                            $"data: {json}\n\n";

                        byte[] bytes = Encoding.UTF8.GetBytes(sseBlock);
                        await httpContext.Response.Body.WriteAsync(bytes, ct);
                        await httpContext.Response.Body.FlushAsync(ct);

                        // Wait before sending the next heartbeat.
                        // TEMPLATE NOTE: Replace Task.Delay with await foreach over a channel reader
                        // to push domain events as they occur, instead of on a fixed schedule.
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), ct);
                        }
                        catch (OperationCanceledException)
                        {
                            // Client disconnected or server is shutting down — exit cleanly.
                            break;
                        }
                    }
                })
            .AllowAnonymous()
            .WithTags("Events")
            .WithName("StreamEvents")
            .WithSummary("Stream server-sent events")
            .WithDescription(
                "Opens a long-lived SSE stream. The server pushes heartbeat events every 5 seconds. " +
                "In a real application replace the heartbeat with domain events from a Channel<T> or event bus. " +
                "Content-Type: text/event-stream.");
    }
}
