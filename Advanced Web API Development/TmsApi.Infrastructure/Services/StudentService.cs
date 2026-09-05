using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;

    public StudentService(TmsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResponse<StudentDto>> GetStudentsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
            .Include(s => s.User);

        IQueryable<Student> filteredQuery = query;
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            filteredQuery = filteredQuery.Where(s =>
                s.FirstName.ToLower().Contains(search) ||
                s.LastName.ToLower().Contains(search) ||
                s.RegistrationNumber.ToLower().Contains(search) ||
                s.Email.ToLower().Contains(search));
        }

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        filteredQuery = request.SortBy?.ToLower() switch
        {
            "name" or "fullname" => request.SortDescending
                ? filteredQuery.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName)
                : filteredQuery.OrderBy(s => s.FirstName).ThenBy(s => s.LastName),
            "firstname" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.FirstName) : filteredQuery.OrderBy(s => s.FirstName),
            "lastname" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.LastName) : filteredQuery.OrderBy(s => s.LastName),
            "studentid" or "registrationnumber" or "regnumber" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.RegistrationNumber) : filteredQuery.OrderBy(s => s.RegistrationNumber),
            "email" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Email) : filteredQuery.OrderBy(s => s.Email),
            "courses" or "coursesenrolled" or "enrolledcourses" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Enrollments.Count) : filteredQuery.OrderBy(s => s.Enrollments.Count),
            "date" or "enrollmentdate" or "registrationdate" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.EnrollmentDate) : filteredQuery.OrderBy(s => s.EnrollmentDate),
            "gpa" => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Gpa) : filteredQuery.OrderBy(s => s.Gpa),
            _ => request.SortDescending ? filteredQuery.OrderByDescending(s => s.Id) : filteredQuery.OrderBy(s => s.Id)
        };

        var items = await filteredQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(s =>
        {
            var dto = _mapper.Map<StudentDto>(s);
            dto.CoursesEnrolled = s.Enrollments.Count;
            dto.Status = !string.IsNullOrWhiteSpace(s.Status) ? s.Status : "Active";
            dto.Avatar = !string.IsNullOrEmpty(s.User?.ProfilePictureUrl)
                ? s.User.ProfilePictureUrl
                : "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80";
            return dto;
        }).ToList();

        return new PagedResponse<StudentDto>(dtos, request.Page, request.PageSize, totalCount);
    }

    public async Task<StudentDto?> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return student == null ? null : _mapper.Map<StudentDto>(student);
    }

    public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto, CancellationToken cancellationToken = default)
    {
        var student = _mapper.Map<Student>(dto);
        student.EnrollmentDate = DateTime.UtcNow;

        await _context.Students.AddAsync(student, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<StudentDto>(student);
    }

    public async Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.FindAsync(new object[] { id }, cancellationToken);
        if (student == null) return false;

        _mapper.Map(dto, student);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
