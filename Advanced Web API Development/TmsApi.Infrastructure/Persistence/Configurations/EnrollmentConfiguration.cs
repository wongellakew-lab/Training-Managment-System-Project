using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();

        builder.Property(e => e.Grade).HasPrecision(5, 2);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property<DateTime>("LastUpdated").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
