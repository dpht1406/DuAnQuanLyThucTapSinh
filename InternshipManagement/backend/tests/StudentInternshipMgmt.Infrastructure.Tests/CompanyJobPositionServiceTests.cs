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

public class CompanyJobPositionServiceTests
{
    [Fact]
    public async Task Company_operations_are_scoped_to_the_claim_company()
    {
        await using var db = CreateDbContext();
        await AddTwoCompanyPositions(db);
        var service = new JobPositionService(db);

        var list = await service.GetJobPositionsForCompanyAsync(
            1, new JobPositionFilterDto { CompanyId = 2, PageSize = 20 });
        var foreignPosition = await service.GetJobPositionByIdForCompanyAsync(1, 20);
        var update = await service.UpdateJobPositionForCompanyAsync(1, 20, UpdateDto());
        var toggle = await service.ToggleOpenForCompanyAsync(1, 20, false);
        var submit = await service.SubmitForApprovalAsync(1, 20);
        var delete = await service.DeleteJobPositionForCompanyAsync(1, 20);

        list.Items.Select(position => position.Id).Should().Equal(10);
        foreignPosition.Should().BeNull();
        update.NotFound.Should().BeTrue();
        toggle.NotFound.Should().BeTrue();
        submit.NotFound.Should().BeTrue();
        delete.NotFound.Should().BeTrue();
        (await db.JobPositions.SingleAsync(position => position.Id == 20)).CompanyId.Should().Be(2);
    }

    [Fact]
    public async Task Company_can_create_draft_and_submit_it_only_once()
    {
        await using var db = CreateDbContext();
        var company = CreateCompany(1);
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        var service = new JobPositionService(db);

        var created = await service.CreateJobPositionForCompanyAsync(1, CreateDto());
        var submitted = await service.SubmitForApprovalAsync(1, created.Data!.Id);
        var submittedAgain = await service.SubmitForApprovalAsync(1, created.Data.Id);

        created.Success.Should().BeTrue();
        created.Data.ApprovalStatus.Should().Be(JobPositionApprovalStatus.Draft);
        submitted.Success.Should().BeTrue();
        (await db.JobPositions.SingleAsync()).ApprovalStatus.Should().Be(JobPositionApprovalStatus.PendingApproval);
        submittedAgain.Success.Should().BeFalse();
        submittedAgain.Error.Should().Contain("Nháp hoặc Bị từ chối");
    }

    [Fact]
    public async Task Company_can_resubmit_rejected_position_and_clears_approval_note()
    {
        await using var db = CreateDbContext();
        var company = CreateCompany(1);
        var position = CreatePosition(10, company);
        position.ApprovalStatus = JobPositionApprovalStatus.Rejected;
        position.ApprovalNote = "Please revise the description.";
        db.AddRange(company, position);
        await db.SaveChangesAsync();

        var result = await new JobPositionService(db).SubmitForApprovalAsync(1, 10);

        result.Success.Should().BeTrue();
        var saved = await db.JobPositions.SingleAsync(item => item.Id == 10);
        saved.ApprovalStatus.Should().Be(JobPositionApprovalStatus.PendingApproval);
        saved.ApprovalNote.Should().BeNull();
    }

    [Fact]
    public async Task Updating_approved_position_requeues_only_content_changes()
    {
        await using var db = CreateDbContext();
        await AddTwoCompanyPositions(db);
        var position = await db.JobPositions.SingleAsync(item => item.Id == 10);
        position.ApprovalStatus = JobPositionApprovalStatus.Approved;
        var service = new JobPositionService(db);
        var originalDeadline = DateTime.Today.AddDays(20);

        var nonContentChange = await service.UpdateJobPositionForCompanyAsync(
            1, 10, UpdateDto(quantity: 2, deadline: originalDeadline));
        var contentChange = await service.UpdateJobPositionForCompanyAsync(
            1, 10, UpdateDto(title: "New title", quantity: 2, deadline: originalDeadline));

        nonContentChange.Success.Should().BeTrue();
        nonContentChange.Data!.ApprovalStatus.Should().Be(JobPositionApprovalStatus.Approved);
        contentChange.Success.Should().BeTrue();
        contentChange.Data!.ApprovalStatus.Should().Be(JobPositionApprovalStatus.PendingApproval);
        (await db.JobPositions.SingleAsync(position => position.Id == 10)).ApprovalStatus
            .Should().Be(JobPositionApprovalStatus.PendingApproval);
    }

    [Fact]
    public async Task Company_cannot_reduce_quantity_below_accepted_count()
    {
        await using var db = CreateDbContext();
        var company = CreateCompany(1);
        var position = CreatePosition(10, company);
        db.AddRange(company, position);
        db.Students.AddRange(
            CreateAcceptedStudent("SV000001", company, position),
            CreateAcceptedStudent("SV000002", company, position));
        await db.SaveChangesAsync();
        var service = new JobPositionService(db);

        var result = await service.UpdateJobPositionForCompanyAsync(1, 10, UpdateDto(quantity: 1));

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("dưới số lượng đã nhận hiện tại (2)");
    }

    [Fact]
    public async Task Company_can_always_close_but_can_open_only_approved_positions()
    {
        await using var db = CreateDbContext();
        await AddTwoCompanyPositions(db);
        var service = new JobPositionService(db);

        var rejectedOpen = await service.ToggleOpenForCompanyAsync(1, 10, true);
        var close = await service.ToggleOpenForCompanyAsync(1, 10, false);
        var openApproved = await service.ToggleOpenForCompanyAsync(2, 20, true);

        rejectedOpen.Success.Should().BeFalse();
        rejectedOpen.Error.Should().Contain("đã được duyệt");
        close.Success.Should().BeTrue();
        (await db.JobPositions.SingleAsync(position => position.Id == 10)).IsOpen.Should().BeFalse();
        openApproved.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Company_cannot_delete_position_with_linked_student()
    {
        await using var db = CreateDbContext();
        var company = CreateCompany(1);
        var position = CreatePosition(10, company);
        db.AddRange(company, position);
        db.Students.Add(CreateAcceptedStudent("SV000001", company, position));
        await db.SaveChangesAsync();

        var result = await new JobPositionService(db).DeleteJobPositionForCompanyAsync(1, 10);

        result.Success.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        (await db.JobPositions.AnyAsync(item => item.Id == 10)).Should().BeTrue();
    }

    [Fact]
    public async Task Admin_created_positions_default_to_approved()
    {
        await using var db = CreateDbContext();
        db.Companies.Add(CreateCompany(1));
        await db.SaveChangesAsync();

        var created = await new JobPositionService(db).CreateJobPositionAsync(new CreateJobPositionDto
        {
            CompanyId = 1,
            Title = "Admin-created position",
            Department = "Engineering",
            Quantity = 1,
            Description = "Description"
        });

        created.Success.Should().BeTrue();
        (await db.JobPositions.SingleAsync()).ApprovalStatus.Should().Be(JobPositionApprovalStatus.Approved);
    }

    [Fact]
    public void Company_role_is_not_authorized_for_admin_user_job_position_or_company_endpoints()
    {
        var protectedMethods = new[]
        {
            typeof(JobPositionsController).GetMethod(nameof(JobPositionsController.GetJobPositions))!,
            typeof(JobPositionsController).GetMethod(nameof(JobPositionsController.GetJobPositionById))!,
            typeof(CompaniesController).GetMethod(nameof(CompaniesController.GetCompanies))!,
            typeof(CompaniesController).GetMethod(nameof(CompaniesController.GetIndustries))!,
            typeof(CompaniesController).GetMethod(nameof(CompaniesController.GetCompanyById))!
        };

        foreach (var method in protectedMethods)
        {
            var authorize = method.GetCustomAttribute<AuthorizeAttribute>();
            authorize.Should().NotBeNull();
            authorize!.Roles!.Split(',').Should().NotContain("Company");
        }
    }

    private static CreateCompanyJobPositionDto CreateDto() => new()
    {
        Title = "Company position",
        Department = "Engineering",
        Quantity = 1,
        Description = "Description",
        IsOpen = true
    };

    private static UpdateCompanyJobPositionDto UpdateDto(
        string title = "Position 10",
        int quantity = 1,
        DateTime? deadline = null) => new()
    {
        Title = title,
        Department = "Engineering",
        Quantity = quantity,
        Deadline = deadline,
        Description = "Description 10",
        IsOpen = true
    };

    private static Company CreateCompany(int id) => new()
    {
        Id = id,
        Name = $"Company {id}",
        Address = "Company address",
        Industry = "Technology",
        ContactPerson = "Contact"
    };

    private static JobPosition CreatePosition(int id, Company company) => new()
    {
        Id = id,
        CompanyId = company.Id,
        Company = company,
        Title = $"Position {id}",
        Department = "Engineering",
        Quantity = 1,
        Description = $"Description {id}",
        IsOpen = true,
        ApprovalStatus = company.Id == 1
            ? JobPositionApprovalStatus.PendingApproval
            : JobPositionApprovalStatus.Approved
    };

    private static Student CreateAcceptedStudent(string code, Company company, JobPosition position) => new()
    {
        StudentCode = code,
        FullName = "Accepted student",
        Email = $"{code}@example.com",
        Major = "Engineering",
        ClassName = "SE01",
        Status = StudentStatus.Accepted,
        CompanyId = company.Id,
        Company = company,
        JobPositionId = position.Id,
        JobPosition = position,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static async Task AddTwoCompanyPositions(AppDbContext db)
    {
        var companyA = CreateCompany(1);
        var companyB = CreateCompany(2);
        db.Companies.AddRange(companyA, companyB);
        db.JobPositions.AddRange(CreatePosition(10, companyA), CreatePosition(20, companyB));
        await db.SaveChangesAsync();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
