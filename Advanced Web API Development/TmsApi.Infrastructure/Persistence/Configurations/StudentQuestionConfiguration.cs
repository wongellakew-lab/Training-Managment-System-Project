using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class StudentQuestionConfiguration : IEntityTypeConfiguration<StudentQuestion>
{
    public void Configure(EntityTypeBuilder<StudentQuestion> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Question).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.Answer).HasMaxLength(3000);

        builder.HasOne(q => q.Student)
            .WithMany()
            .HasForeignKey(q => q.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Course)
            .WithMany()
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.AnsweredByInstructor)
            .WithMany()
            .HasForeignKey(q => q.AnsweredByInstructorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
