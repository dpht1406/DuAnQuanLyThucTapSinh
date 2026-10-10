using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Domain.Enums;
using StudentInternshipMgmt.Infrastructure.Persistence.Seed;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

public class JobPositionConfiguration : IEntityTypeConfiguration<JobPosition>
{
    public void Configure(EntityTypeBuilder<JobPosition> builder)
    {
        builder.ToTable("JobPositions");

        builder.HasKey(jp => jp.Id);

        builder.Property(jp => jp.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(jp => jp.Department)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(jp => jp.Location)
            .HasMaxLength(300);

        builder.Property(jp => jp.Deadline)
            .HasColumnType("date");

        builder.Property(jp => jp.Quantity)
            .IsRequired();

        builder.Property(jp => jp.Description)
            .HasMaxLength(2000);

        builder.Property(jp => jp.IsOpen)
            .IsRequired();

        builder.Property(jp => jp.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(JobPositionApprovalStatus.Approved)
            .HasSentinel(JobPositionApprovalStatus.Approved)
            .IsRequired();

        builder.Property(jp => jp.ApprovalNote)
            .HasMaxLength(1000);

        builder.HasOne(jp => jp.Company)
            .WithMany(c => c.JobPositions)
            .HasForeignKey(jp => jp.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedData.JobPositions);
    }
}
