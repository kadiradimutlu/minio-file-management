using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FileManagement.Identity.Api.OpenApi;

public sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider
        authenticationSchemeProvider) :
    IOpenApiDocumentTransformer
{
    private const string BearerSchemeName =
        "Bearer";

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes =
            await authenticationSchemeProvider
                .GetAllSchemesAsync();

        var bearerIsRegistered =
            authenticationSchemes.Any(
                scheme =>
                    scheme.Name ==
                    BearerSchemeName);

        if (!bearerIsRegistered)
        {
            return;
        }

        document.Components ??=
            new OpenApiComponents();

        document.Components
            .SecuritySchemes ??=
            new Dictionary<
                string,
                IOpenApiSecurityScheme>();

        document.Components
            .SecuritySchemes[
                BearerSchemeName] =
            new OpenApiSecurityScheme
            {
                Type =
                    SecuritySchemeType.Http,
                Scheme = "bearer",
                In =
                    ParameterLocation.Header,
                BearerFormat = "JWT",
                Description =
                    "Enter the JWT access token without the Bearer prefix."
            };
    }
}
