using TmsApi.Application.DTOs.GradeDtos;

namespace TmsApi.Application.Interfaces;

public interface IGradeService
{
    Task<object?> GetStudentAcademicRecordAsync(int studentId, CancellationToken cancellationToken = default);
    Task<object?> AssignGradeAsync(AssignGradeRequestDto dto, CancellationToken cancellationToken = default);
    Task<List<object>> BulkAssignGradesAsync(BulkAssignGradesRequestDto request, CancellationToken cancellationToken = default);
    Task<object?> GetCourseGradebookAsync(int courseId, CancellationToken cancellationToken = default);
    Task<object?> SubmitBulkGradebookAsync(BulkGradebookSubmitRequestDto request, CancellationToken cancellationToken = default);
    Task<object?> UpdateGradebookCellAsync(GradebookCellRequestDto request, CancellationToken cancellationToken = default);
}
