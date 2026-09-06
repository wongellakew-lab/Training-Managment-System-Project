using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using Xunit;

namespace TmsApi.Tests.Unit;

public class FinancialAndDashboardAuditTests
{
    private TmsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TmsDbContext(options);
    }

    [Fact]
    public async Task DashboardService_AdminDashboard_RecentActivities_ShouldIdentifyRealActorAndNotSayYour()
    {
        using var context = CreateInMemoryDbContext();

        var student = new Student
        {
            Id = 1,
            FirstName = "Abebe",
            LastName = "Kebede",
            Email = "abebe@example.com",
            RegistrationNumber = "TMS-STU-001"
        };
        var course = new Course
        {
            Id = 1,
            Title = "French for Beginners",
            Code = "LAN-101",
            Status = "Active"
        };
        var enrollment = new Enrollment
        {
            Id = 1,
            StudentId = 1,
            Student = student,
            CourseId = 1,
            Course = course,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        context.Students.Add(student);
        context.Courses.Add(course);
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new DashboardService(context, mapperMock.Object);
        var adminDashboard = await service.GetAdminDashboardAsync(CancellationToken.None);

        adminDashboard.Should().NotBeNull();
        adminDashboard.RecentActivities.Should().NotBeEmpty();
        var act = adminDashboard.RecentActivities.First();
        act.ActorName.Should().Be("Abebe Kebede");
        act.ActorRole.Should().Be("Student");
        act.Description.Should().Contain("Abebe Kebede");
        act.Description.Should().Contain("French for Beginners");
        act.Description.Should().NotContain("Your registration");
    }

    [Fact]

    public async Task ScheduleService_GetSchedulesAsync_ShouldProjectRecurringSchedulesWithinDateRange()
    {
        using var context = CreateInMemoryDbContext();

        var course = new Course
        {
            Id = 1,
            Title = "Full Stack Web Development",
            Code = "FSWD-101",
            Category = "Web Dev",
            StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Courses.Add(course);

        // Schedule on Mondays (DayOfWeek = Monday)
        context.Set<CourseSchedule>().Add(new CourseSchedule
        {
            Id = 10,
            CourseId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = "09:00 AM",
            EndTime = "11:00 AM",
            Room = "Lab 1",
            Location = "Tech Campus"
        });

        await context.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new ScheduleService(context, mapperMock.Object);

        // Get schedules for September 2026
        var schedules = (await service.GetSchedulesAsync(null, 9, 2026, cancellationToken: CancellationToken.None)).ToList();

        schedules.Should().NotBeEmpty();
        // In Sep 2026: Sep 7 is Monday, Sep 14 is Monday, Sep 21 is Monday. Sep 28 is beyond EndDate.
        schedules.Select(s => s.Date.Day).Should().Contain(new[] { 7, 14, 21 });
        schedules.Select(s => s.Date.Day).Should().NotContain(28);
    }
}
