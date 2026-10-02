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

        builder.Property(pr => pr.ApplicantFullName)
            .HasMaxLength(100);

        builder.Property(pr => pr.ApplicantEmail)
            .HasMaxLength(254);

        builder.Property(pr => pr.ApplicantPhone)
            .HasMaxLength(10);

        builder.Property(pr => pr.ApplicantSchool)
            .HasMaxLength(150);

        builder.Property(pr => pr.ApplicantMajor)
            .HasMaxLength(150);

        builder.Property(pr => pr.CvUrl)
            .HasMaxLength(500);

        builder.Property(pr => pr.CoverLetter)
            .HasMaxLength(2000);

        builder.Property(pr => pr.RejectReason)
            .HasMaxLength(1000);

        builder.Property(pr => pr.CreatedAt)
            .IsRequired();

        builder.Property(pr => pr.InterviewLocation)
            .HasMaxLength(500);

        builder.Property(pr => pr.InterviewNote)
            .HasMaxLength(1000);

        builder.Property(pr => pr.ResultNote)
            .HasMaxLength(1000);

        builder.Property(pr => pr.StudentInterviewNote)
            .HasMaxLength(500);

        builder.Property(pr => pr.ResultActorType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(pr => pr.ConfirmationSource)
            .HasMaxLength(500);

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
