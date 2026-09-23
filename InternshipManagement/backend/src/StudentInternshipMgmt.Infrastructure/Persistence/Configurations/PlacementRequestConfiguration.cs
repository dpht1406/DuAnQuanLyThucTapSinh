using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentInternshipMgmt.Domain.Entities;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

public class PlacementRequestConfiguration : IEntityTypeConfiguration<PlacementRequest>
{
    public void Configure(EntityTypeBuilder<PlacementRequest> builder)
    {
        builder.ToTable("PlacementRequests");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(pr => pr.Note)
            .HasMaxLength(1000);

        builder.Property(pr => pr.RejectReason)
            .HasMaxLength(1000);

        builder.Property(pr => pr.CreatedAt)
            .IsRequired();

        builder.HasOne(pr => pr.Student)
            .WithMany(s => s.PlacementRequests)
            .HasForeignKey(pr => pr.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.Company)
            .WithMany()
            .HasForeignKey(pr => pr.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.JobPosition)
            .WithMany()
            .HasForeignKey(pr => pr.JobPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pr => pr.ReviewedByUser)
            .WithMany()
            .HasForeignKey(pr => pr.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(pr => !pr.Student.IsDeleted);
    }
}
