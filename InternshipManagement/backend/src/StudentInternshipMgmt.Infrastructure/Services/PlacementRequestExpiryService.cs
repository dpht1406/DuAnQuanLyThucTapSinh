using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence;

namespace StudentInternshipMgmt.Infrastructure.Services;

public sealed class PlacementRequestExpiryService : IPlacementRequestExpiryService
{
    private const int BatchSize = 200;
    private const string ExpiryResultNote = "Doanh nghiệp không phản hồi trước hạn";

    private readonly AppDbContext _db;
    private readonly ILogger<PlacementRequestExpiryService> _logger;
    private readonly PlacementRequestEmailNotifier _emailNotifier;

    public PlacementRequestExpiryService(
        AppDbContext db,
        ILogger<PlacementRequestExpiryService> logger)
        : this(
            db,
            logger,
            new PlacementRequestEmailNotifier(
                new NoOpEmailSender(NullLogger<NoOpEmailSender>.Instance),
                NullLogger<PlacementRequestEmailNotifier>.Instance))
    {
    }

    public PlacementRequestExpiryService(
        AppDbContext db,
        ILogger<PlacementRequestExpiryService> logger,
        PlacementRequestEmailNotifier emailNotifier)
    {
        _db = db;
        _logger = logger;
        _emailNotifier = emailNotifier;
    }

    public async Task<int> ExpireOverdueAsync(DateTime utcNow, CancellationToken ct)
    {
        var expiredCount = 0;

        while (true)
        {
            var overdueRequests = await _db.PlacementRequests
                .IgnoreQueryFilters()
                .Include(request => request.Company)
                .Where(request => request.Status == RequestStatus.Pending
                    && request.DueAt.HasValue
                    && request.DueAt.Value < utcNow)
                .OrderBy(request => request.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (overdueRequests.Count == 0)
                break;

            foreach (var request in overdueRequests)
            {
                request.Status = RequestStatus.Expired;
                request.ResultActorType = ActorType.System;
                request.ResultNote = ExpiryResultNote;
                request.ReviewedAt = utcNow;
                request.ReviewedBy = null;
            }

            await _db.SaveChangesAsync(ct);
            expiredCount += overdueRequests.Count;
            foreach (var request in overdueRequests)
                await _emailNotifier.NotifyExpiredAsync(request);
        }

        _logger.LogInformation("Expired {ExpiredRequestCount} overdue placement requests", expiredCount);
        return expiredCount;
    }
}
