using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Api.Controllers;
using StudentInternshipMgmt.Application.Features.JobPositions.Dtos;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class AdminJobPositionApprovalTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Approve_pending_position_preserves_open_flag_and_clears_note(bool isOpen)
    {
        await using var db = CreateDbContext();
        var position = await AddPositionAsync(db, 1, JobPositionApprovalStatus.PendingApproval, isOpen);
        position.ApprovalNote = "Previous review note";
        await db.SaveChangesAsync();

        var result = await new JobPositionService(db).ApproveJobPositionAsync(1);

        result.Success.Should().BeTrue();
        var saved = await db.JobPositions.SingleAsync(item => item.Id == 1);
        saved.ApprovalStatus.Should().Be(JobPositionApprovalStatus.Approved);
        saved.ApprovalNote.Should().BeNull();
        saved.IsOpen.Should().Be(isOpen);
    }

    [Theory]
    [InlineData(JobPositionApprovalStatus.Draft)]
    [InlineData(JobPositionApprovalStatus.Approved)]
    [InlineData(JobPositionApprovalStatus.Rejected)]
    public async Task Approve_rejects_positions_not_pending(JobPositionApprovalStatus status)
    {
        await using var db = CreateDbContext();
        var position = await AddPositionAsync(db, 2, status, isOpen: true);

        var result = await new JobPositionService(db).ApproveJobPositionAsync(2);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        (await db.JobPositions.SingleAsync(item => item.Id == 2)).ApprovalStatus.Should().Be(status);
        position.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task Approve_returns_not_found_for_unknown_position()
    {
        await using var db = CreateDbContext();

        var result = await new JobPositionService(db).ApproveJobPositionAsync(999);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        (await db.JobPositions.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Reject_pending_position_trims_note_and_preserves_open_flag()
    {
        await using var db = CreateDbContext();
        var position = await AddPositionAsync(db, 3, JobPositionApprovalStatus.PendingApproval, isOpen: true);

        var result = await new JobPositionService(db).RejectJobPositionAsync(3, "  Thiếu thông tin tuyển dụng.  ");

        result.Success.Should().BeTrue();
        var saved = await db.JobPositions.SingleAsync(item => item.Id == 3);
        saved.ApprovalStatus.Should().Be(JobPositionApprovalStatus.Rejected);
        saved.ApprovalNote.Should().Be("Thiếu thông tin tuyển dụng.");
        saved.IsOpen.Should().BeTrue();
        position.ApprovalNote.Should().Be("Thiếu thông tin tuyển dụng.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reject_requires_nonblank_note(string note)
    {
        await using var db = CreateDbContext();
        await AddPositionAsync(db, 4, JobPositionApprovalStatus.PendingApproval, isOpen: false);

        var result = await new JobPositionService(db).RejectJobPositionAsync(4, note);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        var saved = await db.JobPositions.SingleAsync(item => item.Id == 4);
        saved.ApprovalStatus.Should().Be(JobPositionApprovalStatus.PendingApproval);
        saved.ApprovalNote.Should().BeNull();
        saved.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Reject_rejects_note_over_1000_characters()
    {
        await using var db = CreateDbContext();
        await AddPositionAsync(db, 5, JobPositionApprovalStatus.PendingApproval, isOpen: true);

        var result = await new JobPositionService(db).RejectJobPositionAsync(5, new string('N', 1001));

        result.Success.Should().BeFalse();
        (await db.JobPositions.SingleAsync(item => item.Id == 5)).ApprovalStatus
            .Should().Be(JobPositionApprovalStatus.PendingApproval);
    }

    [Theory]
    [InlineData(JobPositionApprovalStatus.Draft)]
    [InlineData(JobPositionApprovalStatus.Approved)]
    [InlineData(JobPositionApprovalStatus.Rejected)]
    public async Task Reject_requires_pending_approval_status(JobPositionApprovalStatus status)
    {
        await using var db = CreateDbContext();
        await AddPositionAsync(db, 6, status, isOpen: true);

        var result = await new JobPositionService(db).RejectJobPositionAsync(6, "Valid note");

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        var saved = await db.JobPositions.SingleAsync(item => item.Id == 6);
        saved.ApprovalStatus.Should().Be(status);
        saved.ApprovalNote.Should().BeNull();
        saved.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task Reject_returns_not_found_for_unknown_position()
    {
        await using var db = CreateDbContext();

        var result = await new JobPositionService(db).RejectJobPositionAsync(999, "Valid note");

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        (await db.JobPositions.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Admin_filter_returns_only_pending_approval_positions()
    {
        await using var db = CreateDbContext();
        await AddPositionAsync(db, 10, JobPositionApprovalStatus.PendingApproval, isOpen: true);
        await AddPositionAsync(db, 11, JobPositionApprovalStatus.Draft, isOpen: true);
        await AddPositionAsync(db, 12, JobPositionApprovalStatus.Approved, isOpen: false);
        await AddPositionAsync(db, 13, JobPositionApprovalStatus.Rejected, isOpen: true);

        var result = await new JobPositionService(db).GetJobPositionsAdminAsync(new JobPositionFilterDto
        {
            ApprovalStatus = JobPositionApprovalStatus.PendingApproval,
            PageSize = 20
        });

        result.Items.Select(item => item.Id).Should().Equal(10);
        result.Items.Should().OnlyContain(item => item.ApprovalStatus == JobPositionApprovalStatus.PendingApproval);
    }

    [Fact]
    public void Approval_routes_and_admin_controllers_are_admin_only()
    {
        var approve = typeof(JobPositionsController).GetMethod(nameof(JobPositionsController.ApproveJobPosition));
        var reject = typeof(JobPositionsController).GetMethod(nameof(JobPositionsController.RejectJobPosition));

        approve.Should().NotBeNull();
        reject.Should().NotBeNull();
        approve!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>().Should().ContainSingle(attribute => attribute.Roles == "Admin");
        reject!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>().Should().ContainSingle(attribute => attribute.Roles == "Admin");
        typeof(PlacementRequestsController).GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be("Admin");
        typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be("Admin");
    }

    private static async Task<JobPosition> AddPositionAsync(
        AppDbContext db,
        int id,
        JobPositionApprovalStatus status,
        bool isOpen)
    {
        var company = new Company
        {
            Id = id,
            Name = $"Company {id}",
            Address = "Company address",
            Industry = "Technology",
            ContactPerson = "Contact"
        };
        var position = new JobPosition
        {
            Id = id,
            CompanyId = id,
            Company = company,
            Title = $"Position {id}",
            Department = "Engineering",
            Quantity = 1,
            Description = "Description",
            IsOpen = isOpen,
            ApprovalStatus = status
        };
        db.AddRange(company, position);
        await db.SaveChangesAsync();
        return position;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
