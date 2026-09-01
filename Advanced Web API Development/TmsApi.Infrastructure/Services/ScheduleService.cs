using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private readonly TmsDbContext _context;
    private readonly IMapper _mapper;

    public ScheduleService(TmsDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ScheduleDto>> GetSchedulesAsync(string? category = null, int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Schedules.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(s => s.Category.ToLower() == category.ToLower());
        }

        if (year.HasValue)
        {
            query = query.Where(s => s.Date.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(s => s.Date.Month == month.Value);
        }

        var schedules = await query.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ScheduleDto>>(schedules);
    }

    public async Task<ScheduleDto?> GetScheduleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        return schedule == null ? null : _mapper.Map<ScheduleDto>(schedule);
    }

    public async Task<ScheduleDto> CreateScheduleAsync(CreateScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var schedule = _mapper.Map<Schedule>(dto);
        if (schedule.Date == default)
        {
            schedule.Date = DateTime.UtcNow;
        }

        await _context.Schedules.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ScheduleDto>(schedule);
    }

    public async Task<bool> UpdateScheduleAsync(int id, UpdateScheduleDto dto, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (schedule == null) return false;

        _mapper.Map(dto, schedule);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (schedule == null) return false;

        schedule.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
