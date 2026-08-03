using System.Security.Claims;
using FileManagement.Application.Abstractions.Execution;

namespace FileManagement.Api.Services;

public sealed class HttpFileOperationContext :
    IFileOperationContext
{
    private const string SubjectClaimType =
        "sub";

    private readonly IHttpContextAccessor
        _httpContextAccessor;

    public HttpFileOperationContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor;
    }

    public string ActorUserId
    {
        get
        {
            var user =
                GetHttpContext().User;

            var actorUserId =
                user.FindFirstValue(
                    SubjectClaimType) ??
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrWhiteSpace(
                    actorUserId)
            )
            {
                throw new InvalidOperationException(
                    "Authenticated user id is unavailable.");
            }

            return actorUserId.Trim();
        }
    }

    public string CorrelationId
    {
        get
        {
            var correlationId =
                GetHttpContext()
                    .TraceIdentifier
                    .Trim();

            if (
                string.IsNullOrWhiteSpace(
                    correlationId)
            )
            {
                throw new InvalidOperationException(
                    "Request correlation id is unavailable.");
            }

            return correlationId;
        }
    }

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext ??
            throw new InvalidOperationException(
                "An active HTTP context is required.");
    }
}
