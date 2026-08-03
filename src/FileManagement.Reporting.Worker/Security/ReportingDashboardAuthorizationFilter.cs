using Hangfire.Dashboard;

namespace FileManagement.Reporting.Worker.Security;

public sealed class ReportingDashboardAuthorizationFilter(
    ReportingDashboardCredentialValidator validator)
    : IDashboardAuthorizationFilter
{
    public bool Authorize(
        DashboardContext context)
    {
        var httpContext =
            context.GetHttpContext();

        var authorized =
            validator.IsValid(
                httpContext.Request
                    .Headers.Authorization
                    .ToString());

        if (!authorized)
        {
            httpContext.Response
                .Headers.WWWAuthenticate =
                    "Basic realm=\"File Management Reporting\", charset=\"UTF-8\"";
        }

        return authorized;
    }
}
