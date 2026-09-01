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
        var query = _context.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(search) ||
                s.LastName.ToLower().Contains(search) ||
                s.RegistrationNumber.ToLower().Contains(search) ||
                s.Email.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "firstname" => request.SortDescending ? query.OrderByDescending(s => s.FirstName) : query.OrderBy(s => s.FirstName),
            "lastname" => request.SortDescending ? query.OrderByDescending(s => s.LastName) : query.OrderBy(s => s.LastName),
            "gpa" => request.SortDescending ? query.OrderByDescending(s => s.Gpa) : query.OrderBy(s => s.Gpa),
            _ => query.OrderBy(s => s.Id)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<StudentDto>(_mapper.Map<IEnumerable<StudentDto>>(items), request.Page, request.PageSize, totalCount);
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
