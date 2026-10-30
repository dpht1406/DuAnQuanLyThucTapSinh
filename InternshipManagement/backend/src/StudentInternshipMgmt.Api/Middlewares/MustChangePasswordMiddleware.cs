using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Api.Middlewares;

public sealed class MustChangePasswordMiddleware
{
    private static readonly PathString ChangePasswordPath = new("/api/auth/change-password");
    private static readonly PathString LogoutPath = new("/api/auth/logout");

    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requiresPasswordChange = context.User.Identity?.IsAuthenticated == true
            && context.User.HasClaim("mustChangePassword", "true");
        var allowedPath = HttpMethods.IsPost(context.Request.Method)
            && (context.Request.Path.Equals(ChangePasswordPath, StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.Equals(LogoutPath, StringComparison.OrdinalIgnoreCase));

        if (requiresPasswordChange && !allowedPath)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object>.FailResponse("Bạn cần đổi mật khẩu trước khi sử dụng các chức năng khác."));
            return;
        }

        await _next(context);
    }
}
