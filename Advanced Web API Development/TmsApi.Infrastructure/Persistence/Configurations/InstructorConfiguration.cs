using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.EmployeeNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.EmployeeNumber).IsUnique();

        builder.Property(i => i.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(i => i.LastName).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Email).IsRequired().HasMaxLength(150);
        builder.Property(i => i.Department).HasMaxLength(100);

        builder.Property<DateTime>("LastUpdated").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
