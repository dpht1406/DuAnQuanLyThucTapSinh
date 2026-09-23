using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentInternshipMgmt.Domain.Entities;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

public class StatusHistoryConfiguration : IEntityTypeConfiguration<StatusHistory>
{
    public void Configure(EntityTypeBuilder<StatusHistory> builder)
    {
        builder.ToTable("StatusHistories");

        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.FromStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(sh => sh.ToStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(sh => sh.Note)
            .HasMaxLength(1000);

        builder.Property(sh => sh.ChangedAt)
            .IsRequired();

        builder.HasOne(sh => sh.Student)
            .WithMany(s => s.StatusHistories)
            .HasForeignKey(sh => sh.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sh => sh.Company)
            .WithMany()
            .HasForeignKey(sh => sh.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sh => sh.ChangedByUser)
            .WithMany()
            .HasForeignKey(sh => sh.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sh => !sh.Student.IsDeleted);
    }
}
