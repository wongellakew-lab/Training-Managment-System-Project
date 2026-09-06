using Microsoft.AspNetCore.Http;
using TmsApi.Application.DTOs.AssignmentDtos;

namespace TmsApi.Application.Interfaces;

public interface IAssignmentService
{
    Task<object?> GetCourseAssessmentConfigAsync(int courseId, CancellationToken cancellationToken = default);
    Task<List<AssignmentDto>> GetAssignmentsByCourseAsync(int courseId, CancellationToken cancellationToken = default);
    Task<AssignmentDto?> GetAssignmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAssignmentAsync(CreateAssignmentDto dto, int? instructorId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAssignmentAsync(int id, UpdateAssignmentDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssignmentAsync(int id, CancellationToken cancellationToken = default);
    Task<(string? url, string? fileName)> UploadAssignmentAttachmentAsync(int id, IFormFile file, CancellationToken cancellationToken = default);
    Task<List<AssignmentSubmissionDto>> GetAssignmentSubmissionsAsync(int id, CancellationToken cancellationToken = default);
    Task<AssignmentSubmissionDto?> GetSubmissionDetailAsync(int submissionId, CancellationToken cancellationToken = default);
    Task<bool> GradeSubmissionAsync(int submissionId, GradeSubmissionDto dto, CancellationToken cancellationToken = default);
    Task<List<object>> GetMyAssignmentsAsync(int studentId, CancellationToken cancellationToken = default);
    Task<object?> GetStudentAssignmentDetailAsync(int assignmentId, int studentId, CancellationToken cancellationToken = default);
    Task<object?> SubmitAssignmentAsync(int assignmentId, int studentId, SubmitAssignmentDto dto, CancellationToken cancellationToken = default);
}
