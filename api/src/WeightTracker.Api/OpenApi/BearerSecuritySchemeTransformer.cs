using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WeightTracker.Api.OpenApi;

/// <summary>
/// Declares the bearer scheme on the OpenAPI document.
/// </summary>
/// <remarks>
/// Registering JWT authentication in the service container does not put a security scheme in the
/// document; the built-in OpenAPI generator never looks at the authentication schemes. Without this
/// transformer the document has no <c>securitySchemes</c>, and an API reference UI has no way to
/// offer a token.
/// </remarks>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    /// <summary>Name the scheme is published under, referenced by operations and by the UI.</summary>
    public const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Access token issued by POST /auth/login. Paste the value of the accessToken field.",
        };

        return Task.CompletedTask;
    }
}
