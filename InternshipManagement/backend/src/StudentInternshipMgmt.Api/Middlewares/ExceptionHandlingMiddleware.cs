using System.Net;
using System.Text.Json;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Unhandled {ExceptionType} while processing {Method} {Path}. TraceId: {TraceId}",
                ex.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = ResolveStatusCode(exception);
        var message = exception switch
        {
            ArgumentException => "Dữ liệu gửi lên không hợp lệ.",
            _ when statusCode == HttpStatusCode.InternalServerError =>
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.",
            _ => "Đã xảy ra lỗi trong quá trình xử lý yêu cầu."
        };
        var errors = exception switch
        {
            ArgumentException => new List<string> { "Dữ liệu gửi lên không hợp lệ." },
            _ when statusCode == HttpStatusCode.InternalServerError =>
                new List<string> { $"TraceId: {context.TraceIdentifier}" },
            _ => new List<string> { exception.Message }
        };

        var response = ApiResponse<object>.FailResponse(
            message,
            errors);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(response, JsonOptions);
        return context.Response.WriteAsync(payload);
    }

    private static HttpStatusCode ResolveStatusCode(Exception exception) => exception switch
    {
        ArgumentException => HttpStatusCode.BadRequest,
        KeyNotFoundException => HttpStatusCode.NotFound,
        UnauthorizedAccessException => HttpStatusCode.Unauthorized,
        _ => HttpStatusCode.InternalServerError
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
