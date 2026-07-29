using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FileManagement.Identity.Api.OpenApi;

public sealed class AuthorizationOperationTransformer :
    IOpenApiOperationTransformer
{
    private const string BearerSchemeName =
        "Bearer";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var allowsAnonymousAccess =
            context.Description
                .ActionDescriptor
                .EndpointMetadata
                .OfType<
                    AllowAnonymousAttribute>()
                .Any();

        if (allowsAnonymousAccess)
        {
            return Task.CompletedTask;
        }

        var requiresAuthorization =
            context.Description
                .ActionDescriptor
                .EndpointMetadata
                .OfType<
                    IAuthorizeData>()
                .Any();

        if (!requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security ??=
            [];

        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        BearerSchemeName,
                        context.Document)
                ] = []
            });

        return Task.CompletedTask;
    }
}
