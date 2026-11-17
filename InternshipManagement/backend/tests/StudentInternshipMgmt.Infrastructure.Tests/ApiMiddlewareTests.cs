using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Api.Middlewares;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public sealed class ApiMiddlewareTests
{
    [Fact]
    public async Task Exception_middleware_hides_unexpected_exception_and_returns_trace_id()
    {
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new Exception("secret"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var body = await ReadBodyAsync(context);
        body.Should().NotContain("secret");
        using var response = JsonDocument.Parse(body);
        response.RootElement.GetProperty("message").GetString()
            .Should().Be("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
        response.RootElement.GetProperty("errors").ToString().Should().Contain("trace-123");
    }

    [Fact]
    public async Task Exception_middleware_preserves_not_found_message()
    {
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new KeyNotFoundException("student missing"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var body = await ReadBodyAsync(context);
        body.Should().Contain("student missing");
    }

    [Fact]
    public async Task Exception_middleware_hides_argument_exception_details()
    {
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("Value cannot be null. (Parameter 'secretName')"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadBodyAsync(context);
        body.Should().NotContain("Value cannot be null");
        body.Should().NotContain("secretName");
        using var response = JsonDocument.Parse(body);
        response.RootElement.GetProperty("message").GetString()
            .Should().Be("Dữ liệu gửi lên không hợp lệ.");
    }

    [Fact]
    public async Task Exception_middleware_preserves_unauthorized_message()
    {
        var context = CreateContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new UnauthorizedAccessException("Thông tin xác thực không hợp lệ."),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var body = await ReadBodyAsync(context);
        using var response = JsonDocument.Parse(body);
        response.RootElement.GetProperty("errors")[0].GetString()
            .Should().Be("Thông tin xác thực không hợp lệ.");
    }

    [Fact]
    public async Task Request_logging_writes_one_entry_with_method_path_and_status()
    {
        var context = CreateContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/students";
        context.Request.QueryString = new QueryString("?secret=query-value");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "42") },
            "test"));
        var logger = new TestLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain("GET /api/students responded 200");
        logger.Entries[0].Message.Should().Contain("TraceId=trace-123");
        logger.Entries[0].Message.Should().Contain("UserId=42");
        logger.Entries[0].Message.Should().NotContain("query-value");
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
