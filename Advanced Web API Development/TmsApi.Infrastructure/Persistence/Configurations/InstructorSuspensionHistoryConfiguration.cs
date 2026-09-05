using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class InstructorSuspensionHistoryConfiguration : IEntityTypeConfiguration<InstructorSuspensionHistory>
{
    public void Configure(EntityTypeBuilder<InstructorSuspensionHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasOne(h => h.OriginalInstructor)
            .WithMany()
            .HasForeignKey(h => h.OriginalInstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ReplacementInstructor)
            .WithMany()
            .HasForeignKey(h => h.ReplacementInstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Course)
            .WithMany()
            .HasForeignKey(h => h.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
