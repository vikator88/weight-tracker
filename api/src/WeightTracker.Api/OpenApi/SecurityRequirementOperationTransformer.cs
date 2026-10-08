using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WeightTracker.Api.OpenApi;

/// <summary>
/// Attaches the bearer requirement to the operations that actually need it.
/// </summary>
/// <remarks>
/// Applied per operation rather than once for the whole document, so the reference UI marks
/// <c>GET /workouts</c> as protected while the anonymous auth endpoints stay open. Authorization is
/// read from endpoint metadata, which already carries the controller-level attributes.
/// </remarks>
public sealed class SecurityRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any()
            && metadata.OfType<IAllowAnonymous>().Any() == false;

        if (requiresAuthorization == false)
            return Task.CompletedTask;

        operation.Security ??= [];

        // The reference needs the host document, otherwise it resolves to nothing and the
        // requirement serializes as an empty object.
        var scheme = new OpenApiSecuritySchemeReference(
            BearerSecuritySchemeTransformer.SchemeName, context.Document);

        operation.Security.Add(new OpenApiSecurityRequirement { [scheme] = [] });

        return Task.CompletedTask;
    }
}
