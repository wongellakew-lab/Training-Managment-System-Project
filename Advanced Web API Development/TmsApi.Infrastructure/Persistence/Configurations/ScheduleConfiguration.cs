using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Category).IsRequired().HasMaxLength(50);
        builder.Property(s => s.StartTime).HasMaxLength(20);
        builder.Property(s => s.EndTime).HasMaxLength(20);
        builder.Property(s => s.PicName).HasMaxLength(100);
        builder.Property(s => s.PicAvatar).HasMaxLength(500);
        builder.Property(s => s.Location).HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.ScheduleType).HasMaxLength(50).HasDefaultValue("Lecture");
        builder.Property(s => s.TargetAudience).HasMaxLength(50).HasDefaultValue("All");
        builder.Property(s => s.TargetDepartment).HasMaxLength(100);

        builder.HasOne(s => s.Course)
            .WithMany()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.TargetCourse)
            .WithMany()
            .HasForeignKey(s => s.TargetCourseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
