using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence.Seed;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.MustChangePassword)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        // 1-1 with Student: User.StudentId is the FK and must be unique, but nullable
        // (a User account does not always have to be linked to a Student, e.g. Admin).
        builder.HasIndex(u => u.StudentId)
            .IsUnique()
            .HasDatabaseName("IX_Users_StudentId");

        builder.HasOne(u => u.Student)
            .WithOne(s => s.User)
            .HasForeignKey<User>(u => u.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.CompanyId)
            .IsUnique()
            .HasDatabaseName("IX_Users_CompanyId");

        builder.HasOne(u => u.Company)
            .WithOne(c => c.User)
            .HasForeignKey<User>(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedData.Users);
    }
}
