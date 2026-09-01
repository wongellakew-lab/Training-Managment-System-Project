using System.Dynamic;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Shaping;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class CourseService : ICourseService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;
    private readonly DataShaper<CourseDto> _dataShaper;

    public CourseService(TmsDbContext context, IMapper mapper, DataShaper<CourseDto> dataShaper)
    {
        _context = context;
        _mapper = mapper;
        _dataShaper = dataShaper;
    }

    public async Task<PagedResponse<CourseDto>> GetCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .Where(c => !c.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(search) ||
                c.Code.ToLower().Contains(search) ||
                c.Category.ToLower().Contains(search) ||
                c.Department.ToLower().Contains(search) ||
                (c.Instructor != null && (c.Instructor.FirstName.ToLower().Contains(search) || c.Instructor.LastName.ToLower().Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var dept = request.Department.Trim().ToLower();
            query = query.Where(c => c.Department.ToLower() == dept);
        }

        if (!string.IsNullOrWhiteSpace(request.Category) && request.Category != "All" && request.Category != "All Category")
        {
            var cat = request.Category.Trim().ToLower();
            query = query.Where(c => c.Category.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "All")
        {
            var st = request.Status.Trim().ToLower();
            query = query.Where(c => c.Status.ToLower() == st);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "title" => request.SortDescending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "price" => request.SortDescending ? query.OrderByDescending(c => c.Price) : query.OrderBy(c => c.Price),
            "rating" => request.SortDescending ? query.OrderByDescending(c => c.Rating) : query.OrderBy(c => c.Rating),
            "maxcapacity" => request.SortDescending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
            _ => query.OrderBy(c => c.Id)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<CourseDto>(_mapper.Map<IEnumerable<CourseDto>>(items), request.Page, request.PageSize, totalCount);
    }

    public async Task<PagedResponse<ExpandoObject>> GetShapedCoursesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var response = await GetCoursesAsync(request, cancellationToken);
        var shapedItems = _dataShaper.ShapeData(response.Items, request.Fields);
        return new PagedResponse<ExpandoObject>(shapedItems, response.Page, response.PageSize, response.TotalCount);
    }

    public async Task<CourseDto?> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        return course == null ? null : _mapper.Map<CourseDto>(course);
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var course = _mapper.Map<Course>(dto);
        await _context.Courses.AddAsync(course, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CourseDto>(course);
    }

    public async Task<bool> UpdateCourseAsync(int id, UpdateCourseDto dto, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { id }, cancellationToken);
        if (course == null || course.IsDeleted) return false;

        _mapper.Map(dto, course);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await _context.Courses.FindAsync(new object[] { id }, cancellationToken);
        if (course == null || course.IsDeleted) return false;

        course.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
