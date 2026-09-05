using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
        builder.Property(a => a.MaxScore).HasPrecision(5, 2);
        builder.Property(a => a.ScoredPoints).HasPrecision(5, 2);
        builder.Property(a => a.Status).HasMaxLength(50).HasDefaultValue("Pending");
        builder.Property(a => a.Feedback).HasMaxLength(2000);
        builder.Property(a => a.RubricScoresJson).HasMaxLength(4000);
        builder.Property(a => a.SubmissionFileName).HasMaxLength(255);
        builder.Property(a => a.SubmissionFileType).HasMaxLength(100);

        builder.HasOne(a => a.Student)
            .WithMany(s => s.Assessments)
            .HasForeignKey(a => a.StudentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Course)
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
