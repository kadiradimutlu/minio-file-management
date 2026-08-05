using FileManagement.Reporting.Worker.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FileManagement.Reporting.Worker.OpenApi;

public sealed class BasicSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) :
    IOpenApiDocumentTransformer
{
    public const string SecuritySchemeName =
        "Basic";

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes =
            await authenticationSchemeProvider
                .GetAllSchemesAsync();

        var reportingBasicIsRegistered =
            authenticationSchemes.Any(
                scheme =>
                    scheme.Name ==
                    ReportingBasicAuthenticationHandler
                        .SchemeName);

        if (!reportingBasicIsRegistered)
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
                SecuritySchemeName] =
            new OpenApiSecurityScheme
            {
                Type =
                    SecuritySchemeType.Http,
                Scheme = "basic",
                In =
                    ParameterLocation.Header,
                Description =
                    "Use the reporting dashboard username and password configured in the local environment."
            };
    }
}
