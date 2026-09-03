using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CourseScheduleConfiguration : IEntityTypeConfiguration<CourseSchedule>
{
    public void Configure(EntityTypeBuilder<CourseSchedule> builder)
    {
        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.StartTime)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(cs => cs.EndTime)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(cs => cs.Location)
            .HasMaxLength(150)
            .HasDefaultValue("Main Training Center");

        builder.Property(cs => cs.Room)
            .HasMaxLength(150);

        builder.Property(cs => cs.ScheduleType)
            .HasMaxLength(50)
            .HasDefaultValue("Lecture");

        builder.HasOne(cs => cs.Course)
            .WithMany(c => c.Schedules)
            .HasForeignKey(cs => cs.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(cs => !cs.IsDeleted);
    }
}
