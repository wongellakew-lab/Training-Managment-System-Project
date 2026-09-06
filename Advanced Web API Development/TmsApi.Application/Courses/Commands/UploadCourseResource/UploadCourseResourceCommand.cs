using MediatR;
using Microsoft.AspNetCore.Http;
using TmsApi.Application.DTOs.CourseDtos;

namespace TmsApi.Application.Courses.Commands;

public record UploadCourseResourceCommand(int CourseId, string Title, IFormFile File) : IRequest<CourseResourceDto?>;
