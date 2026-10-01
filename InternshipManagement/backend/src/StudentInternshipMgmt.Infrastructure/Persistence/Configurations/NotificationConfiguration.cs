using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentInternshipMgmt.Domain.Entities;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(notification => notification.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(notification => notification.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(notification => notification.CompanyName)
            .HasMaxLength(200);

        builder.Property(notification => notification.Reason)
            .HasMaxLength(1000);

        builder.Property(notification => notification.CreatedAt)
            .IsRequired();

        builder.HasIndex(notification => new { notification.IsRead, notification.CreatedAt });
    }
}