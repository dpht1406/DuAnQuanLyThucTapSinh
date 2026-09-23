using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Api.Authorization;

public class SameStudentAuthorizationHandler : AuthorizationHandler<SameStudentRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameStudentRequirement requirement)
    {
        if (context.User.IsInRole(UserRole.Admin.ToString()) ||
            context.User.HasClaim("role", UserRole.Admin.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!IsUserRole(context.User))
        {
            return Task.CompletedTask;
        }

        var claimStudentId = context.User.FindFirst("StudentId")?.Value;
        var requestedStudentId = ResolveRequestedStudentId(context.Resource);

        if (int.TryParse(claimStudentId, out var tokenStudentId) &&
            requestedStudentId.HasValue &&
            tokenStudentId == requestedStudentId.Value)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsUserRole(ClaimsPrincipal user)
    {
        return user.IsInRole(UserRole.User.ToString()) ||
               user.HasClaim("role", UserRole.User.ToString());
    }

    private static int? ResolveRequestedStudentId(object? resource)
    {
        if (resource is HttpContext httpContext)
        {
            return TryReadStudentId(httpContext.Request.RouteValues["studentId"])
                   ?? TryReadStudentId(httpContext.Request.RouteValues["id"]);
        }

        return TryReadStudentId(resource);
    }

    private static int? TryReadStudentId(object? value)
    {
        return int.TryParse(value?.ToString(), out var studentId) ? studentId : null;
    }
}
