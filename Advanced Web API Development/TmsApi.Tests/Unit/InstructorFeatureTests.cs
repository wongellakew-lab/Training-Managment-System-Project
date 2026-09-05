using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TmsApi.Api.Controllers.V1;
using TmsApi.Application.Courses.Validators;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.Grading;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Hubs;
using TmsApi.Infrastructure.Persistence;
using Xunit;

namespace TmsApi.Tests.Unit;

public class InstructorFeatureTests
{
    private TmsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TmsDbContext(options);
    }

    [Fact]
    public void UpdateCourseDtoValidator_Draft_AllowsPartialInformationWithValidTitle()
    {
        var validator = new UpdateCourseDtoValidator();
        var dto = new UpdateCourseDto
        {
            Title = "Draft Course 101",
            Status = "Draft",
            DurationHours = 0,
            Category = "",
            Level = "",
            About = ""
        };

        var result = validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateCourseDtoValidator_Draft_RequiresValidTitle()
    {
        var validator = new UpdateCourseDtoValidator();
        var dto = new UpdateCourseDto
        {
            Title = "",
            Status = "Draft"
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void UpdateCourseDtoValidator_Publish_EnforcesAllRequiredFields()
    {
        var validator = new UpdateCourseDtoValidator();
        var dto = new UpdateCourseDto
        {
            Title = "Active Course",
            Status = "Active",
            DurationHours = 0, // Invalid for active
            Category = "",     // Invalid for active
            Level = "",        // Invalid for active
            About = "",        // Invalid for active
            Description = ""   // Invalid for active
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Category);
        result.ShouldHaveValidationErrorFor(x => x.Level);
        result.ShouldHaveValidationErrorFor(x => x.DurationHours);
        result.ShouldHaveValidationErrorFor("About");
    }

    [Fact]
    public void UpdateCourseDtoValidator_Publish_PassesWhenAllFieldsValid()
    {
        var validator = new UpdateCourseDtoValidator();
        var dto = new UpdateCourseDto
        {
            Title = "Full-Stack Development Architecture",
            Status = "Active",
            DurationHours = 40,
            Category = "Web Dev",
            Level = "Intermediate",
            About = "Comprehensive hands-on training.",
            Description = "Full stack modern course.",
            MaxCapacity = 30,
            Schedules = new List<CreateCourseScheduleDto>
            {
                new CreateCourseScheduleDto
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = "09:00 AM",
                    EndTime = "11:00 AM",
                    Location = "Main Campus",
                    Room = "Room 101",
                    ScheduleType = "Lecture"
                }
            }
        };

        var result = validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task BulkSubmitGrades_RejectsNegativeScoresOrScoresExceedingMaxPoints()
    {
        using var context = CreateInMemoryDbContext();
        var hubContext = new Mock<IHubContext<TmsHub, ITmsClient>>();
        var clientsMock = new Mock<IHubClients<ITmsClient>>();
        var clientMock = new Mock<ITmsClient>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientMock.Object);
        clientsMock.Setup(c => c.All).Returns(clientMock.Object);
        hubContext.Setup(h => h.Clients).Returns(clientsMock.Object);

        var gradingService = new GradingService();
        var notificationService = new Mock<INotificationService>();

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        var controller = new GradesController(context, gradingService, notificationService.Object, hubContext.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = adminUser } }
        };

        // Seed course and assignment
        var course = new Course { Id = 101, Title = "Test Course", Code = "TC101" };
        var assignment = new Assignment { Id = 501, CourseId = 101, Title = "Project 1", MaxPoints = 100m, IsPublished = true };
        context.Courses.Add(course);
        context.Assignments.Add(assignment);
        await context.SaveChangesAsync();

        // 1. Negative score
        var negativeRequest = new GradesController.BulkGradebookRequest(101, new List<GradesController.BulkGradebookItem>
        {
            new GradesController.BulkGradebookItem(1, 501, -10m, null)
        });

        var negResult = await controller.SubmitBulkGradebook(negativeRequest, CancellationToken.None);
        negResult.Should().BeOfType<BadRequestObjectResult>();

        // 2. Score > max points
        var overMaxRequest = new GradesController.BulkGradebookRequest(101, new List<GradesController.BulkGradebookItem>
        {
            new GradesController.BulkGradebookItem(1, 501, 120m, null)
        });

        var overResult = await controller.SubmitBulkGradebook(overMaxRequest, CancellationToken.None);
        overResult.Should().BeOfType<BadRequestObjectResult>();

        // 3. Valid score
        var validRequest = new GradesController.BulkGradebookRequest(101, new List<GradesController.BulkGradebookItem>
        {
            new GradesController.BulkGradebookItem(1, 501, 95m, "Excellent work")
        });

        var validResult = await controller.SubmitBulkGradebook(validRequest, CancellationToken.None);
        validResult.Should().BeOfType<OkObjectResult>();

        var saved = await context.Assessments.FirstOrDefaultAsync(a => a.CourseId == 101 && a.StudentId == 1 && a.AssignmentId == 501);
        saved.Should().NotBeNull();
        saved!.ScoredPoints.Should().Be(95m);
        saved.Status.Should().Be("Graded");
        saved.Feedback.Should().Be("Excellent work");
    }

    [Fact]
    public async Task GetCourseAttendance_CorrectlyIdentifiesScheduledVersusUnscheduledDays()
    {
        using var context = CreateInMemoryDbContext();
        var hubContext = new Mock<IHubContext<TmsHub, ITmsClient>>();

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        var controller = new AttendanceController(context, hubContext.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = adminUser } }
        };

        // Course on Mondays only
        var course = new Course
        {
            Id = 202,
            Title = "Database Engineering",
            Code = "DB202",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        context.Courses.Add(course);

        // Monday schedule
        context.CourseSchedules.Add(new CourseSchedule
        {
            Id = 1,
            CourseId = 202,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = "09:00 AM",
            EndTime = "11:00 AM"
        });

        await context.SaveChangesAsync();

        // 2026-09-07 is a Monday
        var monday = new DateTime(2026, 9, 7);
        var monResult = await controller.GetCourseAttendance(202, monday, CancellationToken.None) as OkObjectResult;
        monResult.Should().NotBeNull();

        using var monDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(monResult!.Value));
        monDoc.RootElement.GetProperty("hasScheduledSession").GetBoolean().Should().BeTrue();
        monDoc.RootElement.GetProperty("isRecorded").GetBoolean().Should().BeFalse();

        // 2026-09-08 is a Tuesday (unscheduled)
        var tuesday = new DateTime(2026, 9, 8);
        var tueResult = await controller.GetCourseAttendance(202, tuesday, CancellationToken.None) as OkObjectResult;
        tueResult.Should().NotBeNull();

        using var tueDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(tueResult!.Value));
        tueDoc.RootElement.GetProperty("hasScheduledSession").GetBoolean().Should().BeFalse();
        tueDoc.RootElement.GetProperty("isRecorded").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void CreateCourseDtoValidator_Draft_AllowsPartialInformationWithValidTitle()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "Draft Course From Create",
            Status = "Draft",
            DurationHours = 0,
            Category = "",
            Level = "",
            About = ""
        };

        var result = validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCourseDtoValidator_Publish_EnforcesAllRequiredFields()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "Active Course Create",
            Status = "Active",
            DurationHours = 0,
            Category = "",
            Level = "",
            About = ""
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Category);
        result.ShouldHaveValidationErrorFor(x => x.Level);
        result.ShouldHaveValidationErrorFor(x => x.DurationHours);
        result.ShouldHaveValidationErrorFor("About");
    }

    [Fact]
    public async Task BulkSubmitGrades_RecalculatesEnrollmentGradeAndCompletion()
    {
        using var context = CreateInMemoryDbContext();
        var hubContext = new Mock<IHubContext<TmsHub, ITmsClient>>();
        var clientsMock = new Mock<IHubClients<ITmsClient>>();
        var clientMock = new Mock<ITmsClient>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientMock.Object);
        clientsMock.Setup(c => c.All).Returns(clientMock.Object);
        hubContext.Setup(h => h.Clients).Returns(clientsMock.Object);

        var gradingService = new GradingService();
        var notificationService = new Mock<INotificationService>();

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        var controller = new GradesController(context, gradingService, notificationService.Object, hubContext.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = adminUser } }
        };

        var course = new Course { Id = 303, Title = "Algorithms", Code = "CS303" };
        var a1 = new Assignment { Id = 601, CourseId = 303, Title = "Midterm", MaxPoints = 100m, IsPublished = true };
        var a2 = new Assignment { Id = 602, CourseId = 303, Title = "Final", MaxPoints = 100m, IsPublished = true };
        var student = new Student { Id = 10, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
        var enrollment = new Enrollment { Id = 99, CourseId = 303, StudentId = 10, Status = EnrollmentStatus.Active };

        context.Courses.Add(course);
        context.Assignments.AddRange(a1, a2);
        context.Students.Add(student);
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var request = new GradesController.BulkGradebookRequest(303, new List<GradesController.BulkGradebookItem>
        {
            new GradesController.BulkGradebookItem(10, 601, 90m, null),
            new GradesController.BulkGradebookItem(10, 602, 80m, null)
        });

        var res = await controller.SubmitBulkGradebook(request, CancellationToken.None);
        res.Should().BeOfType<OkObjectResult>();

        var updatedEnrollment = await context.Enrollments.FirstAsync(e => e.Id == 99);
        updatedEnrollment.Grade.Should().Be(85.0m); // (90 + 80) / 200 = 85.0%
        updatedEnrollment.Status.Should().Be(EnrollmentStatus.Completed);
    }

    [Fact]
    public async Task SaveBulkAttendance_UpdatesExistingRecordWithoutDuplication()
    {
        using var context = CreateInMemoryDbContext();
        var hubContext = new Mock<IHubContext<TmsHub, ITmsClient>>();
        var clientsMock = new Mock<IHubClients<ITmsClient>>();
        var clientMock = new Mock<ITmsClient>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientMock.Object);
        clientsMock.Setup(c => c.All).Returns(clientMock.Object);
        hubContext.Setup(h => h.Clients).Returns(clientsMock.Object);

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        var controller = new AttendanceController(context, hubContext.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = adminUser } }
        };

        var course = new Course { Id = 404, Title = "Cloud", Code = "CL404" };
        var student = new Student { Id = 20, FirstName = "Bob", LastName = "Brown", Email = "bob@example.com" };
        var enrollment = new Enrollment { Id = 101, CourseId = 404, StudentId = 20, Status = EnrollmentStatus.Active };

        context.Courses.Add(course);
        context.Students.Add(student);
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var sessionDate = new DateTime(2026, 9, 10, 9, 0, 0);

        // First save: Present (1)
        var firstSave = new AttendanceController.BulkAttendanceRequest(404, null, sessionDate, new List<AttendanceController.BulkAttendanceItem>
        {
            new AttendanceController.BulkAttendanceItem(20, (int)AttendanceStatus.Present, "On time")
        });

        var firstRes = await controller.SaveBulkAttendance(firstSave, CancellationToken.None);
        firstRes.Should().BeOfType<OkObjectResult>();

        var count1 = await context.Attendances.CountAsync(a => a.CourseId == 404 && a.StudentId == 20);
        count1.Should().Be(1);
        var record1 = await context.Attendances.FirstAsync(a => a.CourseId == 404 && a.StudentId == 20);
        record1.Status.Should().Be(AttendanceStatus.Present);

        // Second save for same student & date: Late (2)
        var secondSave = new AttendanceController.BulkAttendanceRequest(404, null, sessionDate, new List<AttendanceController.BulkAttendanceItem>
        {
            new AttendanceController.BulkAttendanceItem(20, (int)AttendanceStatus.Late, "Arrived 15m late")
        });

        var secondRes = await controller.SaveBulkAttendance(secondSave, CancellationToken.None);
        secondRes.Should().BeOfType<OkObjectResult>();

        var count2 = await context.Attendances.CountAsync(a => a.CourseId == 404 && a.StudentId == 20);
        count2.Should().Be(1); // Deduplicated!
        var record2 = await context.Attendances.FirstAsync(a => a.CourseId == 404 && a.StudentId == 20);
        record2.Status.Should().Be(AttendanceStatus.Late);
        record2.Remarks.Should().Be("Arrived 15m late");
    }

    [Fact]
    public void CreateCourseDtoValidator_Publish_WithoutModules_FailsContentValidation()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "DevOps Masterclass",
            Status = "Active",
            DurationHours = 30,
            Category = "DevOps",
            Level = "Advanced",
            About = "Learn full CI/CD pipelines.",
            Description = "Hands-on continuous delivery.",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            MaxCapacity = 25,
            Modules = new List<CourseModuleDto>() // Empty modules
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("CourseContent")
            .WithErrorMessage("Course content is required. Add at least one content item before publishing.");
    }

    [Fact]
    public void CreateCourseDtoValidator_Publish_WithModuleHavingZeroLessons_FailsContentValidation()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "DevOps Masterclass",
            Status = "Active",
            DurationHours = 30,
            Category = "DevOps",
            Level = "Advanced",
            About = "Learn full CI/CD pipelines.",
            Description = "Hands-on continuous delivery.",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            MaxCapacity = 25,
            Modules = new List<CourseModuleDto>
            {
                new CourseModuleDto
                {
                    Title = "Module 1: Getting Started",
                    Lessons = new List<CourseLessonDto>() // 0 lessons
                }
            }
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("CourseContent")
            .WithErrorMessage("Course content is required. Add at least one content item before publishing.");
    }

    [Fact]
    public void CreateCourseDtoValidator_Publish_WithModuleAndLesson_PassesValidation()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "DevOps Masterclass",
            Status = "Active",
            DurationHours = 30,
            Category = "DevOps",
            Level = "Advanced",
            About = "Learn full CI/CD pipelines.",
            Description = "Hands-on continuous delivery.",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            MaxCapacity = 25,
            Modules = new List<CourseModuleDto>
            {
                new CourseModuleDto
                {
                    Title = "Module 1: Introduction",
                    Lessons = new List<CourseLessonDto>
                    {
                        new CourseLessonDto { Title = "Lesson 1: Overview" }
                    }
                }
            }
        };

        var result = validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor("CourseContent");
    }

    [Fact]
    public void CreateCourseDtoValidator_EndDateBeforeStartDate_FailsValidation()
    {
        var validator = new CreateCourseDtoValidator();
        var dto = new CreateCourseDto
        {
            Title = "Date Inversion Course",
            Status = "Draft",
            StartDate = DateTime.UtcNow.Date.AddDays(10),
            EndDate = DateTime.UtcNow.Date.AddDays(5)
        };

        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task UpdateCourseAsync_PreservesInstructorId_WhenPublishedOrUpdatedWithoutExplicitInstructorId()
    {
        using var context = CreateInMemoryDbContext();
        var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<TmsApi.Application.Mappings.MappingProfile>());
        var mapper = mapperConfig.CreateMapper();
        var dataShaper = new TmsApi.Application.Shaping.DataShaper<CourseDto>();
        var service = new TmsApi.Infrastructure.Services.CourseService(context, mapper, dataShaper);

        var course = new Course
        {
            Id = 999,
            Code = "WEB-999",
            Title = "Original Course Title",
            Category = "Web Dev",
            Level = "Beginner",
            Status = "Draft",
            InstructorId = 42,
            DurationHours = 10,
            MaxCapacity = 30
        };
        await context.Courses.AddAsync(course);
        await context.SaveChangesAsync();

        var updateDto = new UpdateCourseDto
        {
            Title = "Updated & Published Web Masterclass",
            Status = "Active",
            Category = "Web Dev",
            Level = "Intermediate",
            DurationHours = 40,
            MaxCapacity = 30,
            InstructorId = null // Simulated frontend publish payload omitting instructorId
        };

        var success = await service.UpdateCourseAsync(999, updateDto);
        success.Should().BeTrue();

        var reloaded = await context.Courses.FindAsync(999);
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(999, "The course record should remain the exact same entity");
        reloaded.InstructorId.Should().Be(42, "InstructorId must be strictly preserved when updating or publishing");
        reloaded.Status.Should().Be("Active");
        reloaded.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void AutoMapper_UpdateCourseDto_DoesNotOverwriteExistingInstructorId_WhenDtoInstructorIdIsNull()
    {
        var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<TmsApi.Application.Mappings.MappingProfile>());
        var mapper = mapperConfig.CreateMapper();

        var existingCourse = new Course
        {
            Id = 555,
            Title = "Cloud Architecture",
            InstructorId = 88
        };

        var dto = new UpdateCourseDto
        {
            Title = "Cloud Architecture Advanced",
            InstructorId = null
        };

        mapper.Map(dto, existingCourse);

        existingCourse.InstructorId.Should().Be(88, "AutoMapper should ignore null InstructorId in UpdateCourseDto");
        existingCourse.Title.Should().Be("Cloud Architecture Advanced");
    }

    [Fact]
    public async Task InstructorsController_GetInstructorById_ExistingInstructor_ReturnsOkWithRealData()
    {
        var context = CreateInMemoryDbContext();
        var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<TmsApi.Application.Mappings.MappingProfile>());
        var mapper = mapperConfig.CreateMapper();
        var scheduleServiceMock = new Mock<IScheduleService>();

        var user = new TmsUser
        {
            Id = "user-inst-1",
            FirstName = "Abebe",
            LastName = "Bikila",
            Email = "abebe.bikila@example.com",
            PhoneNumber = "+251911223344",
            Bio = "Senior Marathon & Sports Instructor",
            ProfilePictureUrl = "https://example.com/avatar.jpg"
        };
        await context.Users.AddAsync(user);

        var instructor = new Instructor
        {
            Id = 101,
            EmployeeNumber = "INS-101",
            FirstName = "Abebe",
            LastName = "Bikila",
            Email = "abebe.bikila@example.com",
            Department = "Athletics",
            HireDate = DateTime.UtcNow.AddYears(-2),
            UserId = user.Id,
            User = user
        };
        await context.Instructors.AddAsync(instructor);

        var course = new Course
        {
            Id = 201,
            Code = "ATH-101",
            Title = "Distance Running Masterclass",
            Category = "Sports",
            Level = "Advanced",
            Status = "Active",
            InstructorId = instructor.Id,
            Instructor = instructor,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(3),
            DurationWeeks = 12,
            LessonsCount = 24
        };
        await context.Courses.AddAsync(course);
        await context.SaveChangesAsync();

        var controller = new InstructorsController(context, mapper, scheduleServiceMock.Object);

        var actionResult = await controller.GetInstructorById(101, CancellationToken.None);
        var okResult = actionResult as OkObjectResult;

        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var dto = okResult.Value as TmsApi.Application.DTOs.InstructorDtos.InstructorDetailDto;
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(101);
        dto.FirstName.Should().Be("Abebe");
        dto.LastName.Should().Be("Bikila");
        dto.Email.Should().Be("abebe.bikila@example.com");
        dto.EmployeeNumber.Should().Be("INS-101");
        dto.PhoneNumber.Should().Be("+251911223344");
        dto.Bio.Should().Be("Senior Marathon & Sports Instructor");
        dto.CoursesCount.Should().Be(1);
        dto.Courses.Should().HaveCount(1);
        dto.Courses[0].Title.Should().Be("Distance Running Masterclass");
        dto.Courses[0].Code.Should().Be("ATH-101");
    }

    [Fact]
    public async Task InstructorsController_GetInstructorById_NonExistentInstructor_ReturnsNotFound()
    {
        var context = CreateInMemoryDbContext();
        var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<TmsApi.Application.Mappings.MappingProfile>());
        var mapper = mapperConfig.CreateMapper();
        var scheduleServiceMock = new Mock<IScheduleService>();

        var controller = new InstructorsController(context, mapper, scheduleServiceMock.Object);

        var actionResult = await controller.GetInstructorById(99999, CancellationToken.None);
        var notFoundResult = actionResult as NotFoundObjectResult;

        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }
}

