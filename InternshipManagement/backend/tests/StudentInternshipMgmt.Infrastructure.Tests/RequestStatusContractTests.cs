using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using StudentInternshipMgmt.Api.Controllers;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Domain.Enums;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class RequestStatusContractTests
{
    [Fact]
    public void Request_status_does_not_include_legacy_approved()
    {
        Enum.GetNames<RequestStatus>().Should().NotContain("Approved");
    }

    [Fact]
    public void Request_status_numeric_values_are_unchanged()
    {
        ((int)RequestStatus.Pending).Should().Be(0);
        ((int)RequestStatus.Rejected).Should().Be(2);
        ((int)RequestStatus.InterviewInvited).Should().Be(3);
        ((int)RequestStatus.Accepted).Should().Be(4);
        ((int)RequestStatus.Expired).Should().Be(5);
    }

    [Fact]
    public void Placement_requests_controller_does_not_expose_legacy_decision_actions()
    {
        var actions = typeof(PlacementRequestsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);

        actions.Select(action => action.Name)
            .Should()
            .NotContain(new[] { "ApproveRequest", "RejectRequest" });

        var routes = actions
            .SelectMany(action => action.GetCustomAttributes<HttpPostAttribute>(inherit: true))
            .Select(attribute => attribute.Template)
            .Where(template => template is not null);

        routes.Should().NotContain(route =>
            route!.Contains("approve", StringComparison.OrdinalIgnoreCase)
            || route.Contains("reject", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Placement_request_service_does_not_expose_legacy_decision_methods()
    {
        typeof(IPlacementRequestService)
            .GetMethods()
            .Select(method => method.Name)
            .Should()
            .NotContain(new[] { "ApproveRequestAsync", "RejectRequestAsync" });
    }
}
