using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentInternshipMgmt.Application.Features.About;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public class AboutService : IAboutService
{
    private const int AboutPageId = 1;
    private const int MaxJsonLength = 60000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _db;
    private readonly ILogger<AboutService> _logger;

    public AboutService(AppDbContext db, ILogger<AboutService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AboutPageDto> GetAboutAsync()
    {
        var entity = await _db.AboutPages.FirstOrDefaultAsync(a => a.Id == AboutPageId);
        if (entity is null)
        {
            return new AboutPageDto
            {
                Content = AboutContentDefaults.CreateDefault(),
                UpdatedAt = null
            };
        }

        try
        {
            var content = JsonSerializer.Deserialize<AboutContentDto>(entity.ContentJson, JsonOptions);
            if (content is null)
            {
                _logger.LogWarning("ContentJson in AboutPages was deserialized to null. Returning default content.");
                return new AboutPageDto
                {
                    Content = AboutContentDefaults.CreateDefault(),
                    UpdatedAt = entity.UpdatedAt
                };
            }

            return new AboutPageDto
            {
                Content = content,
                UpdatedAt = entity.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize ContentJson from AboutPages. Returning default content.");
            return new AboutPageDto
            {
                Content = AboutContentDefaults.CreateDefault(),
                UpdatedAt = entity.UpdatedAt
            };
        }
    }

    public async Task<(bool Success, List<string>? Errors, AboutPageDto? Data)> UpdateAboutAsync(
        AboutContentDto content, int? userId)
    {
        var validationErrors = AboutContentValidator.Validate(content);
        if (validationErrors.Count > 0)
        {
            return (false, validationErrors, null);
        }

        var trimmedContent = AboutContentValidator.TrimContent(content);
        var json = JsonSerializer.Serialize(trimmedContent, JsonOptions);

        if (json.Length > MaxJsonLength)
        {
            return (false, new List<string> { $"Tổng kích thước nội dung ({json.Length} ký tự) vượt quá giới hạn cho phép ({MaxJsonLength} ký tự)." }, null);
        }

        var entity = await _db.AboutPages.FirstOrDefaultAsync(a => a.Id == AboutPageId);
        var now = DateTime.UtcNow;

        if (entity is null)
        {
            var newEntity = new AboutPage
            {
                Id = AboutPageId,
                ContentJson = json,
                UpdatedAt = now,
                UpdatedByUserId = userId
            };

            _db.AboutPages.Add(newEntity);

            try
            {
                await _db.SaveChangesAsync();
                return (true, null, new AboutPageDto
                {
                    Content = trimmedContent,
                    UpdatedAt = newEntity.UpdatedAt
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Duplicate key on creating AboutPage row. Falling back to update existing row.");
                _db.Entry(newEntity).State = EntityState.Detached;

                entity = await _db.AboutPages.FirstOrDefaultAsync(a => a.Id == AboutPageId);
                if (entity is null)
                {
                    throw;
                }
            }
        }

        entity.ContentJson = json;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();

        return (true, null, new AboutPageDto
        {
            Content = trimmedContent,
            UpdatedAt = entity.UpdatedAt
        });
    }

    public async Task<AboutPageDto> ResetAboutAsync(int? userId)
    {
        var defaultContent = AboutContentDefaults.CreateDefault();
        var json = JsonSerializer.Serialize(defaultContent, JsonOptions);
        var now = DateTime.UtcNow;

        var entity = await _db.AboutPages.FirstOrDefaultAsync(a => a.Id == AboutPageId);
        if (entity is null)
        {
            var newEntity = new AboutPage
            {
                Id = AboutPageId,
                ContentJson = json,
                UpdatedAt = now,
                UpdatedByUserId = userId
            };

            _db.AboutPages.Add(newEntity);

            try
            {
                await _db.SaveChangesAsync();
                return new AboutPageDto
                {
                    Content = defaultContent,
                    UpdatedAt = newEntity.UpdatedAt
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Duplicate key on resetting AboutPage row. Falling back to update existing row.");
                _db.Entry(newEntity).State = EntityState.Detached;

                entity = await _db.AboutPages.FirstOrDefaultAsync(a => a.Id == AboutPageId);
                if (entity is null)
                {
                    throw;
                }
            }
        }

        entity.ContentJson = json;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();

        return new AboutPageDto
        {
            Content = defaultContent,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
