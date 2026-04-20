namespace RestBlueprint.Api;

/// <summary>
/// Marker interface that every endpoint class must implement.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint discovery mechanism (<see cref="Extensions.EndpointExtension.MapApplicationEndpoints"/>)
/// scans the assembly at startup for all non-abstract, non-interface types that implement
/// <see cref="IEndpoint"/> and calls <see cref="MapEndpoints"/> on each one.
/// </para>
/// <para>
/// <b>How to add a new endpoint group:</b>
/// <list type="number">
///   <item>Create a new class in <c>Endpoints/</c> that implements <see cref="IEndpoint"/>.</item>
///   <item>Implement <see cref="MapEndpoints"/> and call <c>routeBuilder.MapGroup("your-resource")</c>.</item>
///   <item>The class is picked up automatically — no manual registration is required.</item>
/// </list>
/// </para>
/// </remarks>
public interface IEndpoint
{
    /// <summary>Registers all routes for this endpoint group onto the given route builder.</summary>
    void MapEndpoints(IEndpointRouteBuilder routeBuilder);
}
