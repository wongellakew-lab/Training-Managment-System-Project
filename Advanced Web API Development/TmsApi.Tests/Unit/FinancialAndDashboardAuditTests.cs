using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using TmsApi.Api.Controllers.V1;
using TmsApi.Application.DTOs.FinancialDtos;
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
    public async Task FinancialsController_EmptyDatabase_ShouldReturnZeros()
    {
        using var context = CreateInMemoryDbContext();
        var controller = new FinancialsController(context);

        var result = await controller.GetOverview(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var overview = okResult!.Value as FinancialOverviewDto;
        overview.Should().NotBeNull();
        overview!.TotalRevenue.Should().Be(0);
        overview.TotalPaidOut.Should().Be(0);
        overview.PendingReceivables.Should().Be(0);
        overview.TotalTransactionsCount.Should().Be(0);
        overview.RecentTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task FinancialsController_PopulatedDatabase_ShouldAggregateFromEnrollments()
    {
        using var context = CreateInMemoryDbContext();

        var student = new Student
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@eduva.com",
            RegistrationNumber = "REG-001"
        };
        var course = new Course
        {
            Id = 1,
            Title = "Cloud Architecture",
            Code = "CLOUD-101",
            Credits = 3,
            Department = "Engineering"
        };
        context.Students.Add(student);
        context.Courses.Add(course);

        // Active enrollment
        context.Enrollments.Add(new Enrollment
        {
            Id = 10,
            StudentId = 1,
            CourseId = 1,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        });

        // Pending enrollment
        context.Enrollments.Add(new Enrollment
        {
            Id = 11,
            StudentId = 1,
            CourseId = 1,
            Status = EnrollmentStatus.Pending,
            EnrolledAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new FinancialsController(context);
        var result = await controller.GetOverview(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var overview = okResult!.Value as FinancialOverviewDto;
        overview.Should().NotBeNull();
        overview!.TotalTransactionsCount.Should().Be(2);
    }

    [Fact]
    public async Task DashboardService_AdminDashboard_LearningActivityHeatmap_ShouldReturn42CellsAndReflectRealDbCounts()
    {
        using var context = CreateInMemoryDbContext();

        // Add an enrollment on a specific day
        context.Enrollments.Add(new Enrollment
        {
            Id = 1,
            StudentId = 1,
            CourseId = 1,
            Status = EnrollmentStatus.Active,
            EnrolledAt = new DateTime(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc) // Monday 10am -> 9am-11am bucket
        });
        await context.SaveChangesAsync();

        var mapperMock = new Mock<IMapper>();
        var service = new DashboardService(context, mapperMock.Object);
        var adminDashboard = await service.GetAdminDashboardAsync(CancellationToken.None);

        adminDashboard.Should().NotBeNull();
        adminDashboard.LearningActivityHeatmap.Should().HaveCount(42); // 6 slots x 7 days
        adminDashboard.LearningActivityHeatmap.All(c => c.Level >= 0 && c.Level <= 4).Should().BeTrue();
    }
}
