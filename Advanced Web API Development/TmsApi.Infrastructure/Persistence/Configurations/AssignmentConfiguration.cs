using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title).IsRequired().HasMaxLength(250);
        builder.Property(a => a.Instructions).HasMaxLength(10000);
        builder.Property(a => a.MaxPoints).HasPrecision(6, 2).HasDefaultValue(100m);
        builder.Property(a => a.AttachmentUrl).HasMaxLength(500);
        builder.Property(a => a.AttachmentFileName).HasMaxLength(250);

        builder.HasOne(a => a.Course)
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.CourseModule)
            .WithMany()
            .HasForeignKey(a => a.CourseModuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.CourseLesson)
            .WithMany()
            .HasForeignKey(a => a.CourseLessonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.CreatedByInstructor)
            .WithMany()
            .HasForeignKey(a => a.CreatedByInstructorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Submissions)
            .WithOne(s => s.Assignment)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
