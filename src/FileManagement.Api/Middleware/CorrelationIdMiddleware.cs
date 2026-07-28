using Serilog.Context;

namespace FileManagement.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName =
        "X-Correlation-ID";

    private const int MaximumCorrelationIdLength =
        128;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        var correlationId =
            ResolveCorrelationId(
                context.Request);

        context.TraceIdentifier =
            correlationId;

        context.Response.OnStarting(
            () =>
            {
                context.Response.Headers[
                    HeaderName] = correlationId;

                return Task.CompletedTask;
            });

        using (
            LogContext.PushProperty(
                "CorrelationId",
                correlationId)
        )
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(
        HttpRequest request)
    {
        var candidate = request.Headers[
                HeaderName]
            .FirstOrDefault()
            ?.Trim();

        return IsValid(candidate)
            ? candidate!
            : Guid.NewGuid().ToString("N");
    }

    private static bool IsValid(
        string? value)
    {
        if (
            string.IsNullOrWhiteSpace(value) ||
            value.Length >
                MaximumCorrelationIdLength
        )
        {
            return false;
        }

        return value.All(
            character =>
                char.IsLetterOrDigit(character) ||
                character is '-' or '_' or '.');
    }
}
