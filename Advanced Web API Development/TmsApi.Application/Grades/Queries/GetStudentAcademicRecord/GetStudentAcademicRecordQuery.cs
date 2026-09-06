using MediatR;

namespace TmsApi.Application.Grades.Queries;

public record GetStudentAcademicRecordQuery(int StudentId) : IRequest<object?>;
