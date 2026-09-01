using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RegistrationNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.RegistrationNumber).IsUnique();

        builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Email).IsRequired().HasMaxLength(150);

        builder.Property<DateTime>("LastUpdated").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
