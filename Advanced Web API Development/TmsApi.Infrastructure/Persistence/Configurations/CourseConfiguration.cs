using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Title).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Department).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Credits).IsRequired();
        builder.Property(c => c.MaxCapacity).IsRequired();
        builder.Property(c => c.ExpectedAssignmentCount).HasDefaultValue(5);
        builder.Property(c => c.AssignmentMaxPoints).HasPrecision(5, 2).HasDefaultValue(10m);
        builder.Property(c => c.HasMidterm).HasDefaultValue(true);
        builder.Property(c => c.MidtermMaxPoints).HasPrecision(5, 2).HasDefaultValue(20m);
        builder.Property(c => c.ExpectedMidtermCount).HasDefaultValue(1);
        builder.Property(c => c.HasFinal).HasDefaultValue(true);
        builder.Property(c => c.FinalMaxPoints).HasPrecision(5, 2).HasDefaultValue(30m);
        builder.Property(c => c.ExpectedFinalCount).HasDefaultValue(1);

        builder.Property<DateTime>("LastUpdated").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasOne(c => c.Instructor)
            .WithMany(i => i.Courses)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
