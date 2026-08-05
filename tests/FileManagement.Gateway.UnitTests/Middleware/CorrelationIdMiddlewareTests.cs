using FileManagement.Gateway.Middleware;
using Microsoft.AspNetCore.Http;

namespace FileManagement.Gateway.UnitTests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidHeader_PreservesCorrelationId()
    {
        const string correlationId =
            "client-correlation_123";

        var context =
            CreateContext();

        context.Request.Headers[
            CorrelationIdMiddleware
                .HeaderName] =
            correlationId;

        string? downstreamHeader =
            null;

        var middleware =
            new CorrelationIdMiddleware(
                downstreamContext =>
                {
                    downstreamHeader =
                        downstreamContext
                            .Request.Headers[
                                CorrelationIdMiddleware
                                    .HeaderName]
                            .ToString();

                    return Task.CompletedTask;
                });

        await middleware.InvokeAsync(
            context);

        Assert.Equal(
            correlationId,
            context.TraceIdentifier);

        Assert.Equal(
            correlationId,
            downstreamHeader);

    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid value!")]
    public async Task InvokeAsync_WithInvalidHeader_GeneratesSafeCorrelationId(
        string headerValue)
    {
        var context =
            CreateContext();

        context.Request.Headers[
            CorrelationIdMiddleware
                .HeaderName] =
            headerValue;

        var middleware =
            new CorrelationIdMiddleware(
                _ =>
                    Task.CompletedTask);

        await middleware.InvokeAsync(
            context);

        var generated =
            context.TraceIdentifier;

        Assert.Equal(
            32,
            generated.Length);

        Assert.True(
            Guid.TryParseExact(
                generated,
                "N",
                out _));

        Assert.Equal(
            generated,
            context.Request.Headers[
                CorrelationIdMiddleware
                    .HeaderName]
                .ToString());

    }

    [Fact]
    public async Task InvokeAsync_WithOversizedHeader_GeneratesNewCorrelationId()
    {
        var context =
            CreateContext();

        context.Request.Headers[
            CorrelationIdMiddleware
                .HeaderName] =
            new string(
                'a',
                129);

        var middleware =
            new CorrelationIdMiddleware(
                _ =>
                    Task.CompletedTask);

        await middleware.InvokeAsync(
            context);

        Assert.Equal(
            32,
            context.TraceIdentifier.Length);
    }

    private static DefaultHttpContext
        CreateContext()
    {
        var context =
            new DefaultHttpContext();

        context.Response.Body =
            new MemoryStream();

        return context;
    }
}
