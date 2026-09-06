using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using FluentValidation.TestHelper;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TmsApi.Api.Controllers.V1;
using TmsApi.Application.Courses.Validators;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Mappings;
using TmsApi.Application.Shaping;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using Xunit;

namespace TmsApi.Tests.Unit;

public class CourseApprovalWorkflowTests
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

    private IMediator CreateMediator(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TmsApi.Application.Courses.Queries.GetCoursesQuery).Assembly));
        configureServices?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public void Validator_RejectsUnderReviewWithoutSchedules()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "Cloud Architecture",
            Status = "Under Review",
            DurationHours = 30,
            Category = "Web Dev",
            Level = "Beginner",
            About = "Cloud fundamentals overview.",
            MaxCapacity = 25,
            Modules = new List<CourseModuleDto>
            {
                new CourseModuleDto
                {
                    Title = "Mod 1",
                    Lessons = new List<CourseLessonDto> { new CourseLessonDto { Title = "Less 1" } }
                }
            },
            Schedules = new List<CreateCourseScheduleDto>() // Empty schedules
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Schedules);
    }

    [Fact]
    public void Validator_RejectsScheduleWithEndTimeBeforeStartTime()
    {
        var validator = new CreateCourseScheduleDtoValidator();
        var sched = new CreateCourseScheduleDto
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = "11:00 AM",
            EndTime = "09:00 AM", // End time before start time
            Location = "Main Lab",
            Room = "101"
        };

        var result = validator.TestValidate(sched);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("End time must be after start time"));
    }

    [Fact]
    public void Validator_RejectsOverlappingSchedulesOnSameDay()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "DevOps Engineering",
            Status = "Under Review",
            DurationHours = 20,
            Category = "Web Dev",
            Level = "Intermediate",
            About = "DevOps pipelines.",
            MaxCapacity = 25,
            Modules = new List<CourseModuleDto>
            {
                new CourseModuleDto
                {
                    Title = "Mod 1",
                    Lessons = new List<CourseLessonDto> { new CourseLessonDto { Title = "Less 1" } }
                }
            },
            Schedules = new List<CreateCourseScheduleDto>
            {
                new CreateCourseScheduleDto
                {
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = "09:00 AM",
                    EndTime = "11:00 AM"
                },
                new CreateCourseScheduleDto
                {
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = "10:00 AM", // Overlaps with 09:00 - 11:00
                    EndTime = "12:00 PM"
                }
            }
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Schedules);
    }

    [Fact]
    public async Task Admin_CanApproveUnderReviewCourse_AndNotifyInstructor()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();
        var dataShaper = new DataShaper<CourseDto>();
        var courseService = new CourseService(context, mapper, dataShaper);

        var hubContext = new Mock<IHubContext<TmsHub, ITmsClient>>();
        var clientsMock = new Mock<IHubClients<ITmsClient>>();
        var clientMock = new Mock<ITmsClient>();
        clientsMock.Setup(c => c.All).Returns(clientMock.Object);
        hubContext.Setup(h => h.Clients).Returns(clientsMock.Object);

        var envMock = new Mock<IWebHostEnvironment>();
        var createValidator = new CreateCourseDtoValidator();
        var updateValidator = new UpdateCourseDtoValidator();
        var notificationService = new Mock<INotificationService>();

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        var courseService = new CourseService(context, mapper, dataShaper, envMock.Object, hubContext.Object, notificationService.Object);
        var mediator = CreateMediator(s => s.AddSingleton<ICourseService>(courseService));
        var controller = new CoursesController(mediator, context)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = adminUser } }
        };

        // Seed instructor with linked user
        var instructor = new Instructor
        {
            Id = 5,
            FirstName = "Sarah",
            LastName = "Connor",
            UserId = "user-instructor-5",
            Email = "sarah@tms.test"
        };
        context.Instructors.Add(instructor);

        // Seed Course Under Review
        var course = new Course
        {
            Id = 101,
            Code = "AI-101",
            Title = "AI Fundamentals",
            Status = "Under Review",
            InstructorId = 5,
            Modules = new List<CourseModule>
            {
                new CourseModule
                {
                    Title = "Module 1",
                    Lessons = new List<CourseLesson> { new CourseLesson { Title = "Lesson 1" } }
                }
            },
            Schedules = new List<CourseSchedule>
            {
                new CourseSchedule
                {
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = "09:00 AM",
                    EndTime = "11:00 AM"
                }
            }
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        // Admin approves course
        var actionResult = await controller.ApproveCourse(101, CancellationToken.None);
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();

        // Verify status in DB is Active
        var updatedCourse = await context.Courses.FindAsync(101);
        updatedCourse!.Status.Should().Be("Active");
        updatedCourse.RejectionReason.Should().BeNull();

        // Verify notification sent to instructor
        notificationService.Verify(n => n.CreateNotificationAsync(
            "user-instructor-5",
            "Course Approved",
            It.Is<string>(s => s.Contains("AI Fundamentals")),
            "Course",
            101,
            "Course",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Admin_CanRejectCourseWithReason_AndNotifyInstructor()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();
        var dataShaper = new DataShaper<CourseDto>();

        var hubContext = new Mock<IHubContext<TmsHub, ITmsClient>>();
        var clientsMock = new Mock<IHubClients<ITmsClient>>();
        var clientMock = new Mock<ITmsClient>();
        clientsMock.Setup(c => c.All).Returns(clientMock.Object);
        hubContext.Setup(h => h.Clients).Returns(clientsMock.Object);

        var envMock = new Mock<IWebHostEnvironment>();
        var createValidator = new CreateCourseDtoValidator();
        var updateValidator = new UpdateCourseDtoValidator();
        var notificationService = new Mock<INotificationService>();

        var courseService = new CourseService(context, mapper, dataShaper, envMock.Object, hubContext.Object, notificationService.Object);
        var mediator = CreateMediator(s => s.AddSingleton<ICourseService>(courseService));

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        var controller = new CoursesController(mediator, context)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = adminUser } }
        };

        var instructor = new Instructor
        {
            Id = 6,
            FirstName = "John",
            LastName = "Doe",
            UserId = "user-instructor-6"
        };
        context.Instructors.Add(instructor);

        var course = new Course
        {
            Id = 102,
            Code = "CYBER-201",
            Title = "Cyber Security",
            Status = "Under Review",
            InstructorId = 6
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var rejectDto = new RejectCourseDto
        {
            Reason = "Schedule overlaps with another assigned course."
        };

        var actionResult = await controller.RejectCourse(102, rejectDto, CancellationToken.None);
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();

        var updatedCourse = await context.Courses.FindAsync(102);
        updatedCourse!.Status.Should().Be("Rejected");
        updatedCourse.RejectionReason.Should().Be("Schedule overlaps with another assigned course.");

        notificationService.Verify(n => n.CreateNotificationAsync(
            "user-instructor-6",
            "Course Rejected",
            It.Is<string>(s => s.Contains("Schedule overlaps with another assigned course.")),
            "Course",
            102,
            "Course",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StudentCatalog_ReturnsOnlyActiveCourses_ExcludingUnderReviewAndRejected()
    {
        using var context = CreateInMemoryDbContext();
        var mapper = CreateMapper();
        var dataShaper = new DataShaper<CourseDto>();
        var courseService = new CourseService(context, mapper, dataShaper);

        var hubContext = new Mock<IHubContext<TmsHub, ITmsClient>>();
        var envMock = new Mock<IWebHostEnvironment>();
        var createValidator = new CreateCourseDtoValidator();
        var updateValidator = new UpdateCourseDtoValidator();
        var notificationService = new Mock<INotificationService>();

        var mediator = CreateMediator(s => s.AddSingleton<ICourseService>(courseService));

        var studentUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "student-1"),
            new Claim(ClaimTypes.Role, "Student")
        }, "TestAuth"));

        var controller = new CoursesController(mediator, context)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = studentUser } }
        };

        // Seed courses with various statuses
        context.Courses.AddRange(
            new Course { Id = 1, Title = "Active Course 1", Code = "ACT-1", Status = "Active" },
            new Course { Id = 2, Title = "Under Review Course", Code = "REV-1", Status = "Under Review" },
            new Course { Id = 3, Title = "Draft Course", Code = "DFT-1", Status = "Draft" },
            new Course { Id = 4, Title = "Rejected Course", Code = "REJ-1", Status = "Rejected" },
            new Course { Id = 5, Title = "Active Course 2", Code = "ACT-2", Status = "Active" }
        );
        await context.SaveChangesAsync();

        var request = new PagedRequest { Page = 1, PageSize = 10 };
        var result = await controller.GetCatalog(request, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var pagedResponse = okResult!.Value as PagedResponse<CourseDto>;
        pagedResponse.Should().NotBeNull();

        // Exactly 2 active courses must be returned
        pagedResponse!.TotalCount.Should().Be(2);
        pagedResponse.Items.Should().OnlyContain(c => c.Status == "Active");
        pagedResponse.Items.Select(c => c.Id).Should().Contain(new[] { 1, 5 });
        pagedResponse.Items.Select(c => c.Id).Should().NotContain(new[] { 2, 3, 4 });
    }
}
