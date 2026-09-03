using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Mappings;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using Xunit;

namespace TmsApi.Tests.Unit;

public class CourseScheduleTests
{
    private TmsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TmsDbContext(options);
    }

    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        return config.CreateMapper();
    }

    [Fact]
    public async Task GetStudentScheduleOverviewAsync_ShouldProjectRecurringDays_OnlyWithinCourseDuration()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();

        // 1. Setup Student
        var student = new Student
        {
            Id = 1,
            FirstName = "Alex",
            LastName = "Kim",
            Email = "alex@eduva.com",
            RegistrationNumber = "REG-2026-ALEX"
        };
        context.Students.Add(student);

        // 2. Setup Course: Sep 7, 2026 to Oct 16, 2026 (6 weeks)
        var startDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc); // Monday
        var endDate = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc); // Friday

        var course = new Course
        {
            Id = 10,
            Code = "PROG-101",
            Title = "Introduction to Programming",
            StartDate = startDate,
            EndDate = endDate,
            DurationWeeks = 6,
            Department = "CS",
            Credits = 4,
            MaxCapacity = 30
        };
        context.Courses.Add(course);

        // 3. Setup Recurring Days: Monday, Wednesday, Friday (3:00 - 4:00 PM)
        context.CourseSchedules.AddRange(
            new CourseSchedule { Id = 1, CourseId = 10, DayOfWeek = DayOfWeek.Monday, StartTime = "03:00 PM", EndTime = "04:00 PM", Location = "Main Campus", Room = "Computer Lab 4", ScheduleType = "Lecture" },
            new CourseSchedule { Id = 2, CourseId = 10, DayOfWeek = DayOfWeek.Wednesday, StartTime = "03:00 PM", EndTime = "04:00 PM", Location = "Main Campus", Room = "Computer Lab 4", ScheduleType = "Lecture" },
            new CourseSchedule { Id = 3, CourseId = 10, DayOfWeek = DayOfWeek.Friday, StartTime = "03:00 PM", EndTime = "04:00 PM", Location = "Main Campus", Room = "Computer Lab 4", ScheduleType = "Lab" }
        );

        // 4. Student Enrolls in Course
        context.Enrollments.Add(new Enrollment
        {
            Id = 1,
            StudentId = 1,
            CourseId = 10,
            Status = EnrollmentStatus.Active,
            EnrolledAt = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc)
        });

        await context.SaveChangesAsync();

        var service = new ScheduleService(context, mapper);

        // September 2026 Query
        var sepOverview = await service.GetStudentScheduleOverviewAsync(1, 2026, 9, null, CancellationToken.None);

        sepOverview.Should().NotBeNull();
        sepOverview.EnrolledCoursesCount.Should().Be(1);
        sepOverview.Schedules.Should().NotBeEmpty();

        // Verify all occurrences in September are >= Sep 7 (StartDate) and match Mon/Wed/Fri
        foreach (var occurrence in sepOverview.Schedules)
        {
            occurrence.Date.Should().BeOnOrAfter(startDate);
            occurrence.Date.DayOfWeek.Should().Match(d => d == DayOfWeek.Monday || d == DayOfWeek.Wednesday || d == DayOfWeek.Friday);
            occurrence.StartTime.Should().Be("03:00 PM");
            occurrence.EndTime.Should().Be("04:00 PM");
            occurrence.Location.Should().Be("Main Campus");
            occurrence.Room.Should().Be("Computer Lab 4");
            occurrence.CourseTitle.Should().Be("Introduction to Programming");
        }

        // Before course start (Sep 1 to Sep 6, 2026) must contain 0 occurrences
        sepOverview.Schedules.Any(s => s.Date < startDate).Should().BeFalse();

        // October 2026 Query
        var octOverview = await service.GetStudentScheduleOverviewAsync(1, 2026, 10, null, CancellationToken.None);

        octOverview.Should().NotBeNull();
        // Verify no occurrences in October occur AFTER Oct 16 (EndDate)
        octOverview.Schedules.All(s => s.Date <= endDate).Should().BeTrue();
        octOverview.Schedules.Any(s => s.Date > endDate).Should().BeFalse();

        // November 2026 Query (Course has ended)
        var novOverview = await service.GetStudentScheduleOverviewAsync(1, 2026, 11, null, CancellationToken.None);
        novOverview.Should().NotBeNull();
        novOverview.ClassesThisMonth.Should().Be(0);
        novOverview.Schedules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentScheduleOverviewAsync_MultipleCourses_ShouldCombineSchedules()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();

        var student = new Student { Id = 2, FirstName = "Beth", LastName = "Miller", Email = "beth@eduva.com", RegistrationNumber = "REG-BETH" };
        context.Students.Add(student);

        // Course A: Mon/Wed
        var courseA = new Course
        {
            Id = 21,
            Code = "CS101",
            Title = "Course A",
            StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc),
            DurationWeeks = 4,
            Credits = 3,
            MaxCapacity = 25
        };
        // Course B: Tue/Thu
        var courseB = new Course
        {
            Id = 22,
            Code = "ART201",
            Title = "Course B",
            StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc),
            DurationWeeks = 4,
            Credits = 3,
            MaxCapacity = 25
        };
        context.Courses.AddRange(courseA, courseB);

        context.CourseSchedules.AddRange(
            new CourseSchedule { Id = 10, CourseId = 21, DayOfWeek = DayOfWeek.Monday, StartTime = "03:00 PM", EndTime = "04:00 PM" },
            new CourseSchedule { Id = 11, CourseId = 21, DayOfWeek = DayOfWeek.Wednesday, StartTime = "03:00 PM", EndTime = "04:00 PM" },
            new CourseSchedule { Id = 12, CourseId = 22, DayOfWeek = DayOfWeek.Tuesday, StartTime = "10:00 AM", EndTime = "11:30 AM" },
            new CourseSchedule { Id = 13, CourseId = 22, DayOfWeek = DayOfWeek.Thursday, StartTime = "10:00 AM", EndTime = "11:30 AM" }
        );

        // Enrolled in both
        context.Enrollments.Add(new Enrollment { Id = 10, StudentId = 2, CourseId = 21, Status = EnrollmentStatus.Active });
        context.Enrollments.Add(new Enrollment { Id = 11, StudentId = 2, CourseId = 22, Status = EnrollmentStatus.Active });
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, mapper);
        var overview = await service.GetStudentScheduleOverviewAsync(2, 2026, 9, null, CancellationToken.None);

        overview.EnrolledCoursesCount.Should().Be(2);
        overview.Schedules.Any(s => s.CourseId == 21).Should().BeTrue();
        overview.Schedules.Any(s => s.CourseId == 22).Should().BeTrue();
    }

    [Fact]
    public async Task GetStudentScheduleOverviewAsync_WhenEnrollmentRejectedOrDropped_ShouldNotAppear()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();

        var student = new Student { Id = 3, FirstName = "Carl", LastName = "Vance", Email = "carl@eduva.com", RegistrationNumber = "REG-CARL" };
        var course = new Course
        {
            Id = 30,
            Code = "PHY301",
            Title = "Quantum Physics",
            StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc),
            DurationWeeks = 4,
            Credits = 4,
            MaxCapacity = 20
        };
        context.Students.Add(student);
        context.Courses.Add(course);
        context.CourseSchedules.Add(new CourseSchedule { Id = 20, CourseId = 30, DayOfWeek = DayOfWeek.Monday, StartTime = "09:00 AM", EndTime = "11:00 AM" });

        // Enrollment is Rejected
        context.Enrollments.Add(new Enrollment { Id = 25, StudentId = 3, CourseId = 30, Status = EnrollmentStatus.Rejected });
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, mapper);
        var overview = await service.GetStudentScheduleOverviewAsync(3, 2026, 9, null, CancellationToken.None);

        overview.ClassesThisMonth.Should().Be(0);
        overview.Schedules.Should().BeEmpty();
    }
}
