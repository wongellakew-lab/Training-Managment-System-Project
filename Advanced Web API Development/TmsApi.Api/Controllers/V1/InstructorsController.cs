using Asp.Versioning;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class InstructorsController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;

    public InstructorsController(TmsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetInstructors([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _context.Instructors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(i =>
                i.FirstName.ToLower().Contains(search) ||
                i.LastName.ToLower().Contains(search) ||
                i.Department.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(i => i.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<InstructorDto>(_mapper.Map<IEnumerable<InstructorDto>>(items), request.Page, request.PageSize, totalCount));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInstructorById(int id, CancellationToken cancellationToken)
    {
        var instructor = await _context.Instructors.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (instructor == null) return NotFound(new { message = $"Instructor with ID {id} not found." });

        return Ok(_mapper.Map<InstructorDto>(instructor));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateInstructor([FromBody] CreateInstructorDto dto, CancellationToken cancellationToken)
    {
        var instructor = _mapper.Map<Instructor>(dto);
        await _context.Instructors.AddAsync(instructor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetInstructorById), new { version = "1.0", id = instructor.Id }, _mapper.Map<InstructorDto>(instructor));
    }
}
