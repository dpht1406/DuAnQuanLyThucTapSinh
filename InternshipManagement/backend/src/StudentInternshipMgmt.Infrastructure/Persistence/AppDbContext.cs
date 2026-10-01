using Microsoft.EntityFrameworkCore;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

namespace StudentInternshipMgmt.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PlacementRequest> PlacementRequests => Set<PlacementRequest>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new JobPositionConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new PlacementRequestConfiguration());
        modelBuilder.ApplyConfiguration(new StatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
    }
}
