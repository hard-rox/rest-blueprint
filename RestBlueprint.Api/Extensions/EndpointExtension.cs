namespace RestBlueprint.Api.Extensions;

/// <summary>
/// Extension method for auto-discovering and registering all <see cref="IEndpoint"/> implementations.
/// </summary>
/// <remarks>
/// Uses reflection to find every non-abstract, non-interface type in the entry assembly that
/// implements <see cref="IEndpoint"/>, instantiates it via <see cref="Activator.CreateInstance"/>,
/// and calls <see cref="IEndpoint.MapEndpoints"/>.
///
/// Because discovery is convention-based you only need to implement <see cref="IEndpoint"/> in a
/// new class — no manual registration is required.
/// </remarks>
internal static class EndpointExtension
{
    /// <summary>
    /// Discovers all <see cref="IEndpoint"/> implementations in the calling assembly and registers
    /// their routes onto <paramref name="routeBuilder"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        Type[] endpointTypes =
        [
            .. typeof(Program).Assembly
                .GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                               type.IsAssignableTo(typeof(IEndpoint)))
        ];

        foreach (Type type in endpointTypes)
        {
            IEndpoint endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoints(routeBuilder);
        }

        return routeBuilder;
    }
}
