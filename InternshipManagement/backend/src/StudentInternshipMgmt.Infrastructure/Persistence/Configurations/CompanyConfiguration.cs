using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentInternshipMgmt.Domain.Entities;
using StudentInternshipMgmt.Infrastructure.Persistence.Seed;

namespace StudentInternshipMgmt.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.Industry)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.ContactPerson)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasData(SeedData.Companies);
    }
}
