using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FileManagement.Reporting.Worker.Security;

public sealed class ReportingBasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ReportingDashboardCredentialValidator validator)
    : AuthenticationHandler<
        AuthenticationSchemeOptions>(
            options,
            logger,
            encoder)
{
    public const string SchemeName =
        "ReportingBasic";

    public const string AdministratorRole =
        "ReportingAdmin";

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        var authorizationHeader =
            Request.Headers.Authorization
                .ToString();

        if (
            string.IsNullOrWhiteSpace(
                authorizationHeader)
        )
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        if (
            !validator.IsValid(
                authorizationHeader)
        )
        {
            return Task.FromResult(
                AuthenticateResult.Fail(
                    "Invalid reporting credentials."));
        }

        Claim[] claims =
        [
            new(
                ClaimTypes.Name,
                "reporting-admin"),
            new(
                ClaimTypes.Role,
                AdministratorRole)
        ];

        var identity =
            new ClaimsIdentity(
                claims,
                SchemeName);

        var principal =
            new ClaimsPrincipal(
                identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(
                ticket));
    }

    protected override async Task
        HandleChallengeAsync(
            AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate =
            "Basic realm=\"File Management Reporting\", charset=\"UTF-8\"";

        await base.HandleChallengeAsync(
            properties);
    }
}
