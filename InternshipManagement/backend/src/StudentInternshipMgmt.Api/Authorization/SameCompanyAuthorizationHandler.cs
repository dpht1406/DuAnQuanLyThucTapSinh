using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Api.Authorization;

public class SameCompanyAuthorizationHandler : AuthorizationHandler<SameCompanyRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameCompanyRequirement requirement)
    {
        if (context.User.IsInRole(UserRole.Admin.ToString()) ||
            context.User.HasClaim("role", UserRole.Admin.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!IsCompanyRole(context.User))
        {
            return Task.CompletedTask;
        }

        var claimCompanyId = context.User.FindFirst("CompanyId")?.Value;
        var requestedCompanyId = ResolveRequestedCompanyId(context.Resource);

        if (int.TryParse(claimCompanyId, out var tokenCompanyId) &&
            requestedCompanyId.HasValue &&
            tokenCompanyId == requestedCompanyId.Value)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsCompanyRole(ClaimsPrincipal user)
    {
        return user.IsInRole(UserRole.Company.ToString()) ||
               user.HasClaim("role", UserRole.Company.ToString());
    }

    private static int? ResolveRequestedCompanyId(object? resource)
    {
        if (resource is HttpContext httpContext)
        {
            return TryReadCompanyId(httpContext.Request.RouteValues["companyId"]);
        }

        return resource is string or sbyte or byte or short or ushort or int or uint or long or ulong
            ? TryReadCompanyId(resource)
            : null;
    }

    private static int? TryReadCompanyId(object? value)
    {
        return int.TryParse(value?.ToString(), out var companyId) ? companyId : null;
    }
}