using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TmsApi.Api.Controllers.V1;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.Mappings;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using Xunit;

namespace TmsApi.Tests.Unit;

public class StudentScheduleTests
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
    public async Task GetStudentScheduleOverviewAsync_WhenStudentHasNoEnrollments_ShouldReturnZeros()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();
        var service = new ScheduleService(context, mapper);

        var result = await service.GetStudentScheduleOverviewAsync(999, 2026, 9, null, CancellationToken.None);

        result.Should().NotBeNull();
        result.ClassesThisMonth.Should().Be(0);
        result.ClassesThisWeek.Should().Be(0);
        result.EnrolledCoursesCount.Should().Be(0);
        result.NextClass.Should().BeNull();
        result.EnrolledCourses.Should().BeEmpty();
        result.Schedules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentScheduleOverviewAsync_ShouldOnlyReturnSchedulesForEnrolledCourses()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();

        // 1. Setup Student
        var student = new Student
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@eduva.com",
            RegistrationNumber = "REG-2026-001"
        };
        context.Students.Add(student);

        // 2. Setup Courses
        var courseA = new Course
        {
            Id = 10,
            Title = "Database Systems",
            Code = "CS201",
            Credits = 3,
            Department = "Computer Science"
        };
        var courseB = new Course
        {
            Id = 20,
            Title = "Advanced Accounting",
            Code = "ACC401",
            Credits = 3,
            Department = "Finance"
        };
        context.Courses.AddRange(courseA, courseB);

        // 3. Student only enrolled in Course A
        context.Enrollments.Add(new Enrollment
        {
            Id = 100,
            StudentId = 1,
            CourseId = 10,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        });

        // 4. Schedules for Course A (Student enrolled) and Course B (Student NOT enrolled)
        context.Schedules.Add(new Schedule
        {
            Id = 101,
            CourseId = 10,
            Title = "Database Systems Lecture",
            Date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            StartTime = "09:00 AM",
            EndTime = "10:30 AM",
            Location = "Lab 101",
            ScheduleType = "Lecture"
        });

        context.Schedules.Add(new Schedule
        {
            Id = 102,
            CourseId = 20,
            Title = "Advanced Accounting Seminar",
            Date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            StartTime = "02:00 PM",
            EndTime = "03:30 PM",
            Location = "Hall B",
            ScheduleType = "Lecture"
        });

        await context.SaveChangesAsync();

        var service = new ScheduleService(context, mapper);
        var overview = await service.GetStudentScheduleOverviewAsync(1, 2026, 9, null, CancellationToken.None);

        overview.Should().NotBeNull();
        overview.ClassesThisMonth.Should().Be(1);
        overview.EnrolledCoursesCount.Should().Be(1);
        overview.Schedules.Should().HaveCount(1);
        overview.Schedules.First().Title.Should().Be("Database Systems Lecture");
        overview.Schedules.First().CourseCode.Should().Be("CS201");
        overview.Schedules.Any(s => s.CourseId == 20).Should().BeFalse();
    }

    [Fact]
    public async Task GetStudentScheduleOverviewAsync_WithCourseFilter_ShouldFilterCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();

        var student = new Student { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@eduva.com", RegistrationNumber = "REG-002" };
        var course1 = new Course { Id = 1, Title = "Web Dev", Code = "WD101", Credits = 3, Department = "CS" };
        var course2 = new Course { Id = 2, Title = "Mobile Dev", Code = "MD101", Credits = 3, Department = "CS" };
        context.Students.Add(student);
        context.Courses.AddRange(course1, course2);

        context.Enrollments.Add(new Enrollment { Id = 1, StudentId = 2, CourseId = 1, Status = EnrollmentStatus.Active });
        context.Enrollments.Add(new Enrollment { Id = 2, StudentId = 2, CourseId = 2, Status = EnrollmentStatus.Active });

        context.Schedules.Add(new Schedule { Id = 1, CourseId = 1, Title = "Web Dev Class", Date = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc), StartTime = "09:00 AM" });
        context.Schedules.Add(new Schedule { Id = 2, CourseId = 2, Title = "Mobile Dev Class", Date = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc), StartTime = "11:00 AM" });
        await context.SaveChangesAsync();

        var service = new ScheduleService(context, mapper);

        // Filter by course 1
        var resultCourse1 = await service.GetStudentScheduleOverviewAsync(2, 2026, 10, 1, CancellationToken.None);
        resultCourse1.Schedules.Should().HaveCount(1);
        resultCourse1.Schedules.First().CourseId.Should().Be(1);

        // Filter by all courses (courseId = null or 0)
        var resultAll = await service.GetStudentScheduleOverviewAsync(2, 2026, 10, 0, CancellationToken.None);
        resultAll.Schedules.Should().HaveCount(2);
    }
}
