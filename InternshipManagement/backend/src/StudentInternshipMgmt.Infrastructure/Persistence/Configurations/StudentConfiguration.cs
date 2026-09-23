using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentInternshipMgmt.Domain.Entities;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Major)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.ClassName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Unique index on StudentCode, only enforced for non-deleted rows.
        builder.HasIndex(s => s.StudentCode)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Students_StudentCode");

        builder.HasOne(s => s.Company)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.JobPosition)
            .WithMany(jp => jp.Students)
            .HasForeignKey(s => s.JobPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter: soft-deleted students are excluded from every query by default.
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
