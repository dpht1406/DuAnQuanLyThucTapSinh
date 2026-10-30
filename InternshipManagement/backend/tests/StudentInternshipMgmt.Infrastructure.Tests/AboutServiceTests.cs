using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Features.About;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;

namespace StudentInternshipMgmt.Infrastructure.Tests;

public class AboutServiceTests
{
    [Fact]
    public async Task GetAboutAsync_returns_default_content_with_null_updated_at_when_table_empty()
    {
        await using var db = CreateDbContext();
        var service = new AboutService(db, NullLogger<AboutService>.Instance);

        var result = await service.GetAboutAsync();

        result.Should().NotBeNull();
        result.UpdatedAt.Should().BeNull();
        result.Content.Name.Should().Be("Công ty Cổ phần Công nghệ Cửu Long");
        result.Content.ShortName.Should().Be("Cửu Long Tech");
        result.Content.Facts.Should().HaveCount(6);
    }

    [Fact]
    public async Task UpdateAboutAsync_saves_and_retrieves_content_and_keeps_single_row_on_multiple_saves()
    {
        await using var db = CreateDbContext();
        var service = new AboutService(db, NullLogger<AboutService>.Instance);

        var content = AboutContentDefaults.CreateDefault();
        content.Name = "Công ty TNHH Phần Mềm Mới";
        content.Tagline = "Khẩu hiệu công nghệ mới thử nghiệm.";

        var updateResult1 = await service.UpdateAboutAsync(content, userId: 42);
        updateResult1.Success.Should().BeTrue();
        updateResult1.Data!.UpdatedAt.Should().NotBeNull();
        updateResult1.Data.Content.Name.Should().Be("Công ty TNHH Phần Mềm Mới");

        // Lưu lần hai
        content.Name = "Công ty TNHH Phần Mềm Đã Cập Nhật";
        var updateResult2 = await service.UpdateAboutAsync(content, userId: 42);
        updateResult2.Success.Should().BeTrue();

        // Kiểm tra database chỉ có đúng 1 dòng
        var rowCount = await db.AboutPages.CountAsync();
        rowCount.Should().Be(1);

        var page = await service.GetAboutAsync();
        page.Content.Name.Should().Be("Công ty TNHH Phần Mềm Đã Cập Nhật");
        page.UpdatedAt.Should().NotBeNull();

        var entity = await db.AboutPages.FirstAsync();
        entity.UpdatedByUserId.Should().Be(42);
    }

    [Fact]
    public async Task UpdateAboutAsync_rejects_invalid_content_and_does_not_modify_database()
    {
        await using var db = CreateDbContext();
        var service = new AboutService(db, NullLogger<AboutService>.Instance);

        // Lưu dữ liệu ban đầu
        var initial = AboutContentDefaults.CreateDefault();
        initial.Name = "Công ty Ban Đầu";
        var initialRes = await service.UpdateAboutAsync(initial, userId: 1);
        initialRes.Success.Should().BeTrue();

        var countBefore = await db.AboutPages.CountAsync();
        var entityBefore = await db.AboutPages.AsNoTracking().FirstAsync();

        // Cố tình gửi nội dung không hợp lệ (name rỗng)
        var invalid = AboutContentDefaults.CreateDefault();
        invalid.Name = "   ";

        var result = await service.UpdateAboutAsync(invalid, userId: 2);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("tên đơn vị tiếp nhận"));

        var countAfter = await db.AboutPages.CountAsync();
        countAfter.Should().Be(countBefore);

        var entityAfter = await db.AboutPages.AsNoTracking().FirstAsync();
        entityAfter.ContentJson.Should().Be(entityBefore.ContentJson);
        entityAfter.UpdatedAt.Should().Be(entityBefore.UpdatedAt);
    }

    [Fact]
    public async Task ResetAboutAsync_resets_content_to_default_and_sets_updated_at()
    {
        await using var db = CreateDbContext();
        var service = new AboutService(db, NullLogger<AboutService>.Instance);

        var custom = AboutContentDefaults.CreateDefault();
        custom.Name = "Công ty Đã Bị Sửa";
        await service.UpdateAboutAsync(custom, userId: 10);

        var resetResult = await service.ResetAboutAsync(userId: 11);

        resetResult.Should().NotBeNull();
        resetResult.Content.Name.Should().Be("Công ty Cổ phần Công nghệ Cửu Long");
        resetResult.UpdatedAt.Should().NotBeNull();

        var entity = await db.AboutPages.FirstAsync();
        entity.UpdatedByUserId.Should().Be(11);
    }

    [Fact]
    public async Task GetAboutAsync_returns_default_content_when_json_is_corrupted()
    {
        await using var db = CreateDbContext();
        var corruptedEntity = new AboutPage
        {
            Id = 1,
            ContentJson = "{ malformed json content !!! }",
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = 1
        };
        db.AboutPages.Add(corruptedEntity);
        await db.SaveChangesAsync();

        var service = new AboutService(db, NullLogger<AboutService>.Instance);

        var result = await service.GetAboutAsync();

        result.Should().NotBeNull();
        result.Content.Name.Should().Be("Công ty Cổ phần Công nghệ Cửu Long");
        result.UpdatedAt.Should().Be(corruptedEntity.UpdatedAt);
    }

    [Fact]
    public void Default_content_passes_validation_and_all_default_icons_are_allowed()
    {
        var defaultContent = AboutContentDefaults.CreateDefault();
        var errors = AboutContentValidator.Validate(defaultContent);

        errors.Should().BeEmpty();

        foreach (var fact in defaultContent.Facts)
            AboutIcons.AllowedIcons.Should().Contain(fact.Icon);

        foreach (var field in defaultContent.Fields)
            AboutIcons.AllowedIcons.Should().Contain(field.Icon);

        foreach (var highlight in defaultContent.Environment.Highlights)
            AboutIcons.AllowedIcons.Should().Contain(highlight.Icon);

        foreach (var val in defaultContent.Values)
            AboutIcons.AllowedIcons.Should().Contain(val.Icon);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validation_boundary_for_name_length(int length, bool expectedValid)
    {
        var content = AboutContentDefaults.CreateDefault();
        content.Name = new string('A', length);

        var errors = AboutContentValidator.Validate(content);
        if (expectedValid)
            errors.Should().NotContain(e => e.Contains("tên đơn vị tiếp nhận"));
        else
            errors.Should().Contain(e => e.Contains("tên đơn vị tiếp nhận"));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(8, true)]
    [InlineData(0, false)]
    [InlineData(9, false)]
    public void Validation_boundary_for_facts_count(int count, bool expectedValid)
    {
        var content = AboutContentDefaults.CreateDefault();
        content.Facts = Enumerable.Range(1, count)
            .Select(i => new AboutFactDto { Icon = "pi pi-calendar", Label = $"L{i}", Value = $"V{i}" })
            .ToList();

        var errors = AboutContentValidator.Validate(content);
        if (expectedValid)
            errors.Should().NotContain(e => e.Contains("Thông tin nhanh: phải có từ 1 đến 8 mục"));
        else
            errors.Should().Contain(e => e.Contains("Thông tin nhanh: phải có từ 1 đến 8 mục"));
    }

    [Fact]
    public void Validation_rejects_duplicate_checklist_ids()
    {
        var content = AboutContentDefaults.CreateDefault();
        content.StudentInfo.Checklist = new List<AboutChecklistItemDto>
        {
            new() { Id = "step-one", Label = "Bước 1" },
            new() { Id = "step-one", Label = "Bước trùng id" }
        };

        var errors = AboutContentValidator.Validate(content);
        errors.Should().Contain(e => e.Contains("trùng lặp"));
    }

    [Fact]
    public void Validation_rejects_unallowed_icon()
    {
        var content = AboutContentDefaults.CreateDefault();
        content.Facts[0].Icon = "pi pi-fake-icon-xyz";

        var errors = AboutContentValidator.Validate(content);
        errors.Should().Contain(e => e.Contains("biểu tượng (icon) không hợp lệ"));
    }

    [Theory]
    [InlineData("user@gmail.con")]
    [InlineData("user@gmial.com")]
    [InlineData("not-an-email")]
    public void Validation_rejects_typo_or_invalid_email(string invalidEmail)
    {
        var content = AboutContentDefaults.CreateDefault();
        content.Contact.Email = invalidEmail;

        var errors = AboutContentValidator.Validate(content);
        errors.Should().Contain(e => e.Contains("Liên hệ:"));
    }

    [Fact]
    public void Validation_rejects_control_characters_in_text_fields()
    {
        var content = AboutContentDefaults.CreateDefault();
        content.Tagline = "Khẩu hiệu\u0007chứa ký tự chuông";

        var errors = AboutContentValidator.Validate(content);
        errors.Should().Contain(e => e.Contains("ký tự điều khiển"));
    }

    [Fact]
    public void Validation_allows_line_breaks_in_summary_and_faq_answer()
    {
        var content = AboutContentDefaults.CreateDefault();
        content.Summary = "Dòng 1\r\nDòng 2\nDòng 3";
        content.StudentInfo.Faq[0].A = "Câu trả lời dòng 1\nCâu trả lời dòng 2";

        var errors = AboutContentValidator.Validate(content);
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("0292 000 0000", true)]
    [InlineData("+84 (28) 1234.5678", true)]
    [InlineData("12345", false)] // ít hơn 8 chữ số
    [InlineData("0292-abc-0000", false)] // chứa ký tự chữ
    public void Validation_handles_contact_phone_rules(string phone, bool expectedValid)
    {
        var content = AboutContentDefaults.CreateDefault();
        content.Contact.Phone = phone;

        var errors = AboutContentValidator.Validate(content);
        if (expectedValid)
            errors.Should().NotContain(e => e.Contains("số điện thoại"));
        else
            errors.Should().Contain(e => e.Contains("số điện thoại"));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
