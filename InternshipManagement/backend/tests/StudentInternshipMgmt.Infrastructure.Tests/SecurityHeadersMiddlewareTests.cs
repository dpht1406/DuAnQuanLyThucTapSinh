using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using StudentInternshipMgmt.Api.Middlewares;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Api_response_receives_security_and_no_cache_headers()
    {
        var context = CreateContext("/api/auth/login");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, new TestEnvironment());

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("no-referrer");
        context.Response.Headers["Permissions-Policy"].ToString().Should().Be("camera=(), microphone=(), geolocation=()");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Be("default-src 'none'; frame-ancestors 'none'");
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        context.Response.Headers.Pragma.ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task Swagger_response_does_not_receive_content_security_policy()
    {
        var context = CreateContext("/swagger/index.html");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, new TestEnvironment());

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        context.Response.Headers.Should().NotContainKey("Content-Security-Policy");
    }

    [Fact]
    public async Task Existing_export_cache_control_is_not_overwritten()
    {
        var context = CreateContext("/api/students/export");
        var middleware = new SecurityHeadersMiddleware(httpContext =>
        {
            httpContext.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        }, new TestEnvironment());

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }

    [Fact]
    public async Task Development_response_does_not_receive_content_security_policy()
    {
        var context = CreateContext("/");
        var middleware = new SecurityHeadersMiddleware(
            _ => Task.CompletedTask,
            new TestEnvironment { EnvironmentName = "Development" });

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        context.Response.Headers.Should().NotContainKey("Content-Security-Policy");
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SecurityHeadersMiddlewareTests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Production";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
    }
}
