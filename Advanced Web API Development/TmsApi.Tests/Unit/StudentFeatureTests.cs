using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TmsApi.Api.Extensions;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using Xunit;

namespace TmsApi.Tests.Unit;

public class StudentFeatureTests
{
    private TmsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TmsDbContext(options);
    }

    [Fact]
    public void TmsUser_StudentRelationship_ShouldBeBidirectional()
    {
        var user = new TmsUser
        {
            Id = "user-123",
            UserName = "student@eduva.com",
            Email = "student@eduva.com",
            FirstName = "Test",
            LastName = "Student"
        };

        var student = new Student
        {
            Id = 42,
            UserId = user.Id,
            User = user,
            FirstName = "Test",
            LastName = "Student",
            Email = "student@eduva.com",
            RegistrationNumber = "REG-TEST-001"
        };

        user.Student = student;
        user.StudentId = student.Id;

        user.Student.Should().NotBeNull();
        user.Student!.UserId.Should().Be("user-123");
        student.User.Should().NotBeNull();
        student.User!.StudentId.Should().Be(42);
    }

    [Fact]
    public void ClaimsExtensions_GetUserId_ShouldExtractSubOrNameIdentifier()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-abc-456"),
            new Claim("studentId", "77")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        principal.GetUserId().Should().Be("user-abc-456");
        principal.GetStudentId().Should().Be(77);
    }

    [Fact]
    public async Task ClaimsExtensions_ResolveStudentIdAsync_ShouldFallbackToDatabaseLookup()
    {
        using var context = CreateInMemoryDbContext();
        var student = new Student
        {
            Id = 88,
            UserId = "user-xyz-999",
            FirstName = "Alex",
            LastName = "Smith",
            Email = "alex@eduva.com",
            RegistrationNumber = "REG-ALEX"
        };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-xyz-999") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var resolvedId = await principal.ResolveStudentIdAsync(context);

        resolvedId.Should().Be(88);
    }

    [Fact]
    public async Task EnrollmentService_ShouldReject_WhenPrerequisiteNotCompleted()
    {
        using var context = CreateInMemoryDbContext();

        var course = new Course
        {
            Id = 101,
            Code = "CS201",
            Title = "Advanced Algorithms",
            Status = "Active",
            MaxCapacity = 30,
            Prerequisites = "CS101"
        };
        context.Courses.Add(course);

        var student = new Student
        {
            Id = 5,
            FirstName = "Bob",
            LastName = "Green",
            Email = "bob@eduva.com",
            RegistrationNumber = "REG-BOB"
        };
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var mapperMock = new Mock<AutoMapper.IMapper>();
        var service = new EnrollmentService(context, mapperMock.Object);

        Func<Task> act = async () => await service.CreateEnrollmentAsync(new Application.DTOs.EnrollmentDtos.CreateEnrollmentDto
        {
            CourseId = 101,
            StudentId = 5
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Prerequisite*CS101*has not been completed*");
    }

    [Fact]
    public async Task EnrollmentService_ShouldReject_WhenMaxCapacityExceeded()
    {
        using var context = CreateInMemoryDbContext();

        var course = new Course
        {
            Id = 102,
            Code = "CS102",
            Title = "Web Dev",
            Status = "Active",
            MaxCapacity = 1,
            Prerequisites = "None"
        };
        context.Courses.Add(course);

        // Pre-existing active enrollment filling capacity
        context.Enrollments.Add(new Enrollment
        {
            Id = 1,
            CourseId = 102,
            StudentId = 10,
            Status = EnrollmentStatus.Active
        });

        var student11 = new Student
        {
            Id = 11,
            FirstName = "Alice",
            LastName = "Wonder",
            Email = "alice@eduva.com",
            RegistrationNumber = "REG-ALICE"
        };
        context.Students.Add(student11);
        await context.SaveChangesAsync();

        var mapperMock = new Mock<AutoMapper.IMapper>();
        var service = new EnrollmentService(context, mapperMock.Object);

        Func<Task> act = async () => await service.CreateEnrollmentAsync(new Application.DTOs.EnrollmentDtos.CreateEnrollmentDto
        {
            CourseId = 102,
            StudentId = 11
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Course is full*");
    }

    [Fact]
    public async Task EnrollmentService_ShouldReactivateEnrollment_WhenPreviouslyDropped()
    {
        using var context = CreateInMemoryDbContext();

        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var course = new Course
        {
            Id = 103,
            Code = "CS103",
            Title = "Database Systems",
            Status = "Active",
            MaxCapacity = 10,
            Prerequisites = "None"
        };
        var student = new Student
        {
            Id = 12,
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@eduva.com",
            RegistrationNumber = "REG-CHARLIE"
        };
        // Pre-existing dropped enrollment
        var droppedEnrollment = new Enrollment
        {
            Id = 99,
            CourseId = 103,
            StudentId = 12,
            Status = EnrollmentStatus.Dropped,
            Grade = 3.5m,
            EnrolledAt = DateTime.UtcNow.AddMonths(-2),
            Batch = "Old Batch",
            PhoneNumber = "111",
            Country = "Old Country"
        };

        context.Courses.Add(course);
        context.Students.Add(student);
        context.Enrollments.Add(droppedEnrollment);
        await context.SaveChangesAsync();

        var service = new EnrollmentService(context, mapper);

        var result = await service.CreateEnrollmentAsync(new Application.DTOs.EnrollmentDtos.CreateEnrollmentDto
        {
            CourseId = 103,
            StudentId = 12,
            Batch = "New Batch",
            PhoneNumber = "+1 555-9999",
            Country = "Canada"
        });

        result.Should().NotBeNull();
        result.Id.Should().Be(99); // Reused same record!
        result.Status.Should().Be(EnrollmentStatus.Active);
        result.Batch.Should().Be("New Batch");
        result.PhoneNumber.Should().Be("+1 555-9999");
        result.Country.Should().Be("Canada");

        var dbEnrollments = await context.Enrollments.IgnoreQueryFilters().Where(e => e.StudentId == 12 && e.CourseId == 103).ToListAsync();
        dbEnrollments.Should().HaveCount(1);
        dbEnrollments[0].Status.Should().Be(EnrollmentStatus.Active);
        dbEnrollments[0].Grade.Should().BeNull();
        dbEnrollments[0].IsDeleted.Should().BeFalse();

        var dbCourse = await context.Courses.FindAsync(103);
        dbCourse!.EnrolledStudentsCount.Should().Be(1);
    }

    [Fact]
    public async Task EnrollmentService_ShouldReject_WhenAlreadyActiveOrPending()
    {
        using var context = CreateInMemoryDbContext();

        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var course = new Course
        {
            Id = 104,
            Code = "CS104",
            Title = "Algorithms",
            Status = "Active",
            MaxCapacity = 10,
            Prerequisites = "None"
        };
        var student = new Student
        {
            Id = 13,
            FirstName = "David",
            LastName = "Miller",
            Email = "david@eduva.com",
            RegistrationNumber = "REG-DAVID"
        };
        var activeEnrollment = new Enrollment
        {
            Id = 100,
            CourseId = 104,
            StudentId = 13,
            Status = EnrollmentStatus.Active
        };

        context.Courses.Add(course);
        context.Students.Add(student);
        context.Enrollments.Add(activeEnrollment);
        await context.SaveChangesAsync();

        var service = new EnrollmentService(context, mapper);

        Func<Task> act = async () => await service.CreateEnrollmentAsync(new Application.DTOs.EnrollmentDtos.CreateEnrollmentDto
        {
            CourseId = 104,
            StudentId = 13
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already enrolled*");
    }

    [Fact]
    public async Task EnrollmentService_ShouldRejectReactivation_WhenCourseIsFull()
    {
        using var context = CreateInMemoryDbContext();

        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var course = new Course
        {
            Id = 105,
            Code = "CS105",
            Title = "Networks",
            Status = "Active",
            MaxCapacity = 1,
            Prerequisites = "None"
        };
        var student1 = new Student
        {
            Id = 14,
            FirstName = "Eve",
            LastName = "Adams",
            Email = "eve@eduva.com",
            RegistrationNumber = "REG-EVE"
        };
        var student2 = new Student
        {
            Id = 15,
            FirstName = "Frank",
            LastName = "Wright",
            Email = "frank@eduva.com",
            RegistrationNumber = "REG-FRANK"
        };
        // Student 1 is active (filling capacity)
        var activeEnrollment = new Enrollment
        {
            Id = 101,
            CourseId = 105,
            StudentId = 14,
            Status = EnrollmentStatus.Active
        };
        // Student 2 was dropped
        var droppedEnrollment = new Enrollment
        {
            Id = 102,
            CourseId = 105,
            StudentId = 15,
            Status = EnrollmentStatus.Dropped
        };

        context.Courses.Add(course);
        context.Students.AddRange(student1, student2);
        context.Enrollments.AddRange(activeEnrollment, droppedEnrollment);
        await context.SaveChangesAsync();

        var service = new EnrollmentService(context, mapper);

        Func<Task> act = async () => await service.CreateEnrollmentAsync(new Application.DTOs.EnrollmentDtos.CreateEnrollmentDto
        {
            CourseId = 105,
            StudentId = 15
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Course is full*");
    }

    [Fact]
    public async Task CourseService_ShouldFilter_BySkillLevel_SubjectMatter_AndAvailability()
    {
        using var context = CreateInMemoryDbContext();

        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        // 3 Courses:
        // Course 1: Beginner, Department "Web Development", MaxCapacity 2, 2 enrolled (FULL)
        var c1 = new Course { Id = 201, Code = "CS101", Title = "HTML & CSS", Level = "Beginner", Department = "Web Development", Category = "Web Dev", MaxCapacity = 2, Status = "Active" };
        // Course 2: Advanced, Department "Computer Science", MaxCapacity 10, 0 enrolled (AVAILABLE)
        var c2 = new Course { Id = 202, Code = "CS301", Title = "Compilers", Level = "Advanced", Department = "Computer Science", Category = "CS", MaxCapacity = 10, Status = "Active" };
        // Course 3: Beginner, Department "Design", MaxCapacity 5, 1 enrolled (AVAILABLE)
        var c3 = new Course { Id = 203, Code = "DES101", Title = "Typography", Level = "Beginner", Department = "Design", Category = "Design", MaxCapacity = 5, Status = "Active" };

        context.Courses.AddRange(c1, c2, c3);

        // Enrollments for C1 (full)
        context.Enrollments.Add(new Enrollment { Id = 2011, CourseId = 201, StudentId = 1, Status = EnrollmentStatus.Active });
        context.Enrollments.Add(new Enrollment { Id = 2012, CourseId = 201, StudentId = 2, Status = EnrollmentStatus.Active });

        // Enrollment for C3 (not full)
        context.Enrollments.Add(new Enrollment { Id = 2031, CourseId = 203, StudentId = 1, Status = EnrollmentStatus.Active });

        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new TmsApi.Application.Shaping.DataShaper<TmsApi.Application.DTOs.CourseDtos.CourseDto>());

        // 1. Filter by Skill Level: "Beginner" -> Should return c1 and c3
        var beginnerResult = await service.GetCoursesAsync(new Application.DTOs.PagedRequest { SkillLevel = "Beginner" });
        beginnerResult.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { 201, 203 });

        // 2. Filter by Subject Matter: "Computer Science" -> Should return c2
        var csResult = await service.GetCoursesAsync(new Application.DTOs.PagedRequest { SubjectMatter = "Computer Science" });
        csResult.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { 202 });

        // 3. Filter by Availability: "Available" -> Should return c2 and c3 (not c1 which is full)
        var availResult = await service.GetCoursesAsync(new Application.DTOs.PagedRequest { Availability = "Available" });
        availResult.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { 202, 203 });

        // 4. Filter by Availability: "Full" -> Should return only c1
        var fullResult = await service.GetCoursesAsync(new Application.DTOs.PagedRequest { Availability = "Full" });
        fullResult.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { 201 });
    }

    [Fact]
    public async Task CourseService_GetCourseByIdAsync_ShouldReturnHierarchicalOutlineAndResources()
    {
        using var context = CreateInMemoryDbContext();

        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var instructor = new Instructor
        {
            Id = 50,
            FirstName = "Hana",
            LastName = "Bekele",
            Department = "Software Engineering",
            Email = "hana.bekele@eduva.com"
        };
        context.Instructors.Add(instructor);

        var course = new Course
        {
            Id = 501,
            Code = "SWE-401",
            Title = "Cloud Native Architecture",
            Level = "Advanced",
            Department = "Software Engineering",
            Category = "Web Dev",
            MaxCapacity = 25,
            DurationWeeks = 8,
            StartDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc),
            InstructorId = 50,
            Instructor = instructor,
            Status = "Active"
        };
        context.Courses.Add(course);

        // Schedules
        context.CourseSchedules.Add(new CourseSchedule
        {
            CourseId = 501,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = "03:00 PM",
            EndTime = "04:30 PM",
            Room = "Lab 101",
            ScheduleType = "Lecture"
        });

        // Modules & Lessons
        var mod1 = new CourseModule
        {
            CourseId = 501,
            Title = "Containerization with Docker",
            Description = "Docker images, containers, and multi-stage builds.",
            OrderIndex = 1
        };
        context.CourseModules.Add(mod1);
        await context.SaveChangesAsync();

        context.CourseLessons.Add(new CourseLesson { CourseModuleId = mod1.Id, Title = "Docker Architecture & OCI Standards", OrderIndex = 1 });
        context.CourseLessons.Add(new CourseLesson { CourseModuleId = mod1.Id, Title = "Dockerfile Best Practices", OrderIndex = 2 });

        // Resources
        context.CourseResources.Add(new CourseResource
        {
            CourseId = 501,
            Title = "Cloud Architecture Blueprint",
            FileType = "PDF",
            FileSize = "2.4 MB",
            Url = "/resources/blueprint.pdf"
        });

        // 5 Enrollments
        for (int i = 1; i <= 5; i++)
        {
            context.Enrollments.Add(new Enrollment { CourseId = 501, StudentId = i, Status = EnrollmentStatus.Active });
        }

        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new TmsApi.Application.Shaping.DataShaper<TmsApi.Application.DTOs.CourseDtos.CourseDto>());

        var result = await service.GetCourseByIdAsync(501);

        result.Should().NotBeNull();
        result!.Code.Should().Be("SWE-401");
        result.InstructorName.Should().Be("Hana Bekele");
        result.InstructorDepartment.Should().Be("Software Engineering");
        result.EnrolledCount.Should().Be(5);
        result.AvailableSeats.Should().Be(20);
        result.IsFull.Should().BeFalse();
        result.Schedules.Should().HaveCount(1);
        result.Schedules[0].DayName.Should().Be("Monday");

        result.Modules.Should().HaveCount(1);
        result.Modules[0].Title.Should().Be("Containerization with Docker");
        result.Modules[0].Lessons.Should().HaveCount(2);
        result.Modules[0].Lessons[0].Title.Should().Be("Docker Architecture & OCI Standards");

        result.Resources.Should().HaveCount(1);
        result.Resources[0].Title.Should().Be("Cloud Architecture Blueprint");
        result.Resources[0].FileType.Should().Be("PDF");
    }

    [Fact]
    public async Task StudentScheduleOverview_ExactThreeWeekMondayCourse_ShouldOnlyAppearOnSep7_14_21()
    {
        using var context = CreateInMemoryDbContext();

        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        // 1. Instructor: John Doe
        var instructor = new Instructor
        {
            Id = 60,
            FirstName = "John",
            LastName = "Doe",
            Department = "Engineering",
            Email = "john.doe@eduva.com"
        };
        context.Instructors.Add(instructor);

        // 2. Course A: 3 Weeks, Sep 7 – Sep 21 2026, Monday 3:00 PM – 4:00 PM
        var courseA = new Course
        {
            Id = 601,
            Code = "CRSA-101",
            Title = "Course A",
            DurationWeeks = 3,
            StartDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc), // Monday
            EndDate = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc),   // Monday (exactly 3 Mondays: 7, 14, 21)
            InstructorId = 60,
            Instructor = instructor,
            Status = "Active"
        };
        context.Courses.Add(courseA);

        var scheduleA = new CourseSchedule
        {
            CourseId = 601,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = "3:00 PM",
            EndTime = "4:00 PM",
            Location = "Main Training Center",
            Room = "Room 204",
            ScheduleType = "Lecture"
        };
        context.CourseSchedules.Add(scheduleA);

        // 3. Student enrolls in Course A
        int studentId = 77;
        context.Enrollments.Add(new Enrollment
        {
            CourseId = 601,
            StudentId = studentId,
            Status = EnrollmentStatus.Active
        });

        await context.SaveChangesAsync();

        var scheduleService = new ScheduleService(context, mapper);

        // 4. Query student's schedule overview for September 2026
        var overview = await scheduleService.GetStudentScheduleOverviewAsync(studentId, year: 2026, month: 9);

        overview.Should().NotBeNull();
        var courseAClasses = overview.Schedules.Where(s => s.CourseId == 601).ToList();

        // Must have exactly 3 occurrences: Sep 7, Sep 14, Sep 21
        courseAClasses.Should().HaveCount(3);

        var sep7 = courseAClasses.FirstOrDefault(s => s.Date.Date == new DateTime(2026, 9, 7));
        sep7.Should().NotBeNull();
        sep7!.StartTime.Should().Be("3:00 PM");
        sep7.EndTime.Should().Be("4:00 PM");
        sep7.Location.Should().Be("Main Training Center");
        sep7.Room.Should().Be("Room 204");
        sep7.InstructorName.Should().Be("John Doe");

        var sep14 = courseAClasses.FirstOrDefault(s => s.Date.Date == new DateTime(2026, 9, 14));
        sep14.Should().NotBeNull();
        sep14!.StartTime.Should().Be("3:00 PM");
        sep14.EndTime.Should().Be("4:00 PM");
        sep14.Location.Should().Be("Main Training Center");
        sep14.Room.Should().Be("Room 204");
        sep14.InstructorName.Should().Be("John Doe");

        var sep21 = courseAClasses.FirstOrDefault(s => s.Date.Date == new DateTime(2026, 9, 21));
        sep21.Should().NotBeNull();
        sep21!.StartTime.Should().Be("3:00 PM");
        sep21.EndTime.Should().Be("4:00 PM");
        sep21.Location.Should().Be("Main Training Center");
        sep21.Room.Should().Be("Room 204");
        sep21.InstructorName.Should().Be("John Doe");

        // MUST NOT contain September 28 (after EndDate)
        courseAClasses.Any(s => s.Date.Date == new DateTime(2026, 9, 28)).Should().BeFalse();

        // 5. Query October 2026: MUST NOT contain October 5 or any other occurrences
        var octoberOverview = await scheduleService.GetStudentScheduleOverviewAsync(studentId, year: 2026, month: 10);
        octoberOverview.Schedules.Where(s => s.CourseId == 601).Should().BeEmpty();
    }

    [Fact]
    public async Task StudentScheduleOverview_MultiDayWeeklyCourse_ShouldProjectAllDaysWithinRange()
    {
        using var context = CreateInMemoryDbContext();

        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        // Course B: 2 Weeks, Monday and Wednesday, Sep 7 – Sep 20 2026
        // Sep 7 (Mon), Sep 9 (Wed), Sep 14 (Mon), Sep 16 (Wed) -> 4 occurrences
        var courseB = new Course
        {
            Id = 602,
            Code = "CRSB-201",
            Title = "Course B Multi-Day",
            DurationWeeks = 2,
            StartDate = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc),
            Status = "Active"
        };
        context.Courses.Add(courseB);

        context.CourseSchedules.Add(new CourseSchedule
        {
            CourseId = 602,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = "10:00 AM",
            EndTime = "11:30 AM",
            Location = "Campus West",
            Room = "Lab 1",
            ScheduleType = "Lecture"
        });

        context.CourseSchedules.Add(new CourseSchedule
        {
            CourseId = 602,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = "01:00 PM",
            EndTime = "03:00 PM",
            Location = "Campus West",
            Room = "Lab 2",
            ScheduleType = "Lab"
        });

        int studentId = 88;
        context.Enrollments.Add(new Enrollment
        {
            CourseId = 602,
            StudentId = studentId,
            Status = EnrollmentStatus.Active
        });

        await context.SaveChangesAsync();

        var scheduleService = new ScheduleService(context, mapper);
        var overview = await scheduleService.GetStudentScheduleOverviewAsync(studentId, year: 2026, month: 9);

        var classes = overview.Schedules.Where(s => s.CourseId == 602).ToList();
        classes.Should().HaveCount(4);

        // Mondays
        classes.Count(c => c.Date.DayOfWeek == DayOfWeek.Monday).Should().Be(2);
        classes.Where(c => c.Date.DayOfWeek == DayOfWeek.Monday).All(c => c.StartTime == "10:00 AM" && c.Room == "Lab 1").Should().BeTrue();

        // Wednesdays
        classes.Count(c => c.Date.DayOfWeek == DayOfWeek.Wednesday).Should().Be(2);
        classes.Where(c => c.Date.DayOfWeek == DayOfWeek.Wednesday).All(c => c.StartTime == "01:00 PM" && c.Room == "Lab 2").Should().BeTrue();
    }

    [Fact]
    public void CourseScheduleValidationHelper_ShouldEnforceDateAndScheduleRules()
    {
        var start = new DateTime(2026, 9, 7);
        var end = new DateTime(2026, 9, 21);

        // Valid
        var valid = Application.Courses.Validators.CourseScheduleValidationHelper.ValidateCourseDatesAndSchedules(
            start, end, durationWeeks: 3,
            new[] { new Application.DTOs.CourseDtos.CourseScheduleDto { DayOfWeek = DayOfWeek.Monday, StartTime = "09:00 AM", EndTime = "10:00 AM" } });
        valid.IsValid.Should().BeTrue();

        // Invalid: End date before start date
        var invalidDates = Application.Courses.Validators.CourseScheduleValidationHelper.ValidateCourseDatesAndSchedules(
            end, start, durationWeeks: 3);
        invalidDates.IsValid.Should().BeFalse();
        invalidDates.ErrorMessage.Should().Contain("earlier than");

        // Invalid: DurationWeeks <= 0
        var invalidDuration = Application.Courses.Validators.CourseScheduleValidationHelper.ValidateCourseDatesAndSchedules(
            start, end, durationWeeks: 0);
        invalidDuration.IsValid.Should().BeFalse();

        // Invalid: End time before start time
        var invalidTimes = Application.Courses.Validators.CourseScheduleValidationHelper.ValidateCourseDatesAndSchedules(
            start, end, durationWeeks: 3,
            new[] { new Application.DTOs.CourseDtos.CourseScheduleDto { DayOfWeek = DayOfWeek.Monday, StartTime = "04:00 PM", EndTime = "03:00 PM" } });
        invalidTimes.IsValid.Should().BeFalse();
        invalidTimes.ErrorMessage.Should().Contain("End time must be after start time");
    }

    [Fact]
    public async Task CourseService_GetStudentEnrolledCoursesAsync_ShouldReturnEnrolledCoursesWithSchedulesAndStatus()
    {
        using var context = CreateInMemoryDbContext();
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        // Instructor
        var instructor = new Instructor { Id = 71, FirstName = "Hana", LastName = "Bekele", Department = "Web Development" };
        context.Instructors.Add(instructor);

        // Course 1: Active
        var c1 = new Course
        {
            Id = 701,
            Code = "ADV-301",
            Title = "Advanced Web Development",
            Department = "Web Development",
            Category = "Engineering",
            Level = "Intermediate",
            Credits = 4,
            DurationWeeks = 6,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(32),
            InstructorId = 71,
            Instructor = instructor,
            Status = "Active"
        };
        // Course 2: Upcoming
        var c2 = new Course
        {
            Id = 702,
            Code = "DEV-401",
            Title = "Cloud Native Architecture",
            Department = "Software Engineering",
            Category = "DevOps",
            Level = "Advanced",
            Credits = 3,
            DurationWeeks = 8,
            StartDate = DateTime.UtcNow.AddDays(15),
            EndDate = DateTime.UtcNow.AddDays(70),
            InstructorId = 71,
            Instructor = instructor,
            Status = "Active"
        };
        // Course 3: Other Student's Course
        var c3 = new Course
        {
            Id = 703,
            Code = "DES-101",
            Title = "UI/UX Foundations",
            Status = "Active"
        };
        context.Courses.AddRange(c1, c2, c3);

        // Schedules for c1: Mon, Wed, Fri
        context.CourseSchedules.AddRange(
            new CourseSchedule { CourseId = 701, DayOfWeek = DayOfWeek.Monday, StartTime = "3:00 PM", EndTime = "4:00 PM", Location = "Main Training Center", Room = "Computer Lab 4" },
            new CourseSchedule { CourseId = 701, DayOfWeek = DayOfWeek.Wednesday, StartTime = "3:00 PM", EndTime = "4:00 PM", Location = "Main Training Center", Room = "Computer Lab 4" },
            new CourseSchedule { CourseId = 701, DayOfWeek = DayOfWeek.Friday, StartTime = "3:00 PM", EndTime = "4:00 PM", Location = "Main Training Center", Room = "Computer Lab 4" }
        );

        // Modules & Lessons for c1
        var mod = new CourseModule { CourseId = 701, Title = "Module 1", OrderIndex = 1 };
        context.CourseModules.Add(mod);
        await context.SaveChangesAsync();

        context.CourseLessons.AddRange(
            new CourseLesson { CourseModuleId = mod.Id, Title = "Lesson 1", OrderIndex = 1 },
            new CourseLesson { CourseModuleId = mod.Id, Title = "Lesson 2", OrderIndex = 2 }
        );

        // Enrollments: Student 99 enrolled in c1 (Active) and c2 (Upcoming)
        int targetStudentId = 99;
        context.Enrollments.AddRange(
            new Enrollment { CourseId = 701, StudentId = targetStudentId, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = 702, StudentId = targetStudentId, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = 703, StudentId = 100, Status = EnrollmentStatus.Active } // Different student
        );

        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new Application.Shaping.DataShaper<Application.DTOs.CourseDtos.CourseDto>());

        var myCourses = await service.GetStudentEnrolledCoursesAsync(targetStudentId);

        myCourses.Should().NotBeNull();
        myCourses.Should().HaveCount(2); // Only targetStudentId courses
        myCourses.Any(c => c.CourseId == 703).Should().BeFalse();

        var advCourse = myCourses.FirstOrDefault(c => c.CourseId == 701);
        advCourse.Should().NotBeNull();
        advCourse!.Title.Should().Be("Advanced Web Development");
        advCourse.Level.Should().Be("Intermediate");
        advCourse.InstructorName.Should().Be("Hana Bekele");
        advCourse.DaysDisplay.Should().Be("Mon \u2022 Wed \u2022 Fri");
        advCourse.TimeDisplay.Should().Be("3:00 PM \u2013 4:00 PM");
        advCourse.Location.Should().Be("Main Training Center");
        advCourse.Room.Should().Be("Computer Lab 4");
        advCourse.AcademicStatus.Should().Be("Active");
        advCourse.TotalLessons.Should().Be(2);

        var upcomingCourse = myCourses.FirstOrDefault(c => c.CourseId == 702);
        upcomingCourse.Should().NotBeNull();
        upcomingCourse!.AcademicStatus.Should().Be("Upcoming");
    }

    [Fact]
    public async Task CourseService_GetStudentCoursesStatisticsAsync_ShouldCalculateAccurateStats()
    {
        using var context = CreateInMemoryDbContext();
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var c1 = new Course { Id = 801, Code = "C1", Title = "C1", Credits = 3, StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(20), Status = "Active" };
        var c2 = new Course { Id = 802, Code = "C2", Title = "C2", Credits = 4, StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(40), Status = "Active" };
        var c3 = new Course { Id = 803, Code = "C3", Title = "C3", Credits = 3, StartDate = DateTime.UtcNow.AddDays(-60), EndDate = DateTime.UtcNow.AddDays(-10), Status = "Active" };

        context.Courses.AddRange(c1, c2, c3);

        int studentId = 120;
        context.Enrollments.AddRange(
            new Enrollment { CourseId = 801, StudentId = studentId, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = 802, StudentId = studentId, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = 803, StudentId = studentId, Status = EnrollmentStatus.Completed, Grade = 92m }
        );

        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new Application.Shaping.DataShaper<Application.DTOs.CourseDtos.CourseDto>());

        var stats = await service.GetStudentCoursesStatisticsAsync(studentId);

        stats.Should().NotBeNull();
        stats.TotalEnrolledCourses.Should().Be(3);
        stats.ActiveCourses.Should().Be(1);
        stats.UpcomingCourses.Should().Be(1);
        stats.CompletedCourses.Should().Be(1);
        stats.TotalCredits.Should().Be(10); // 3 + 4 + 3
    }

    [Fact]
    public void Enrollment_ShouldNotHaveFeeOrPaymentStatusProperties()
    {
        var enrollmentType = typeof(Enrollment);
        enrollmentType.GetProperty("Fee").Should().BeNull("Course and enrollment fee properties must be completely removed");
        enrollmentType.GetProperty("PaymentStatus").Should().BeNull("Course and enrollment payment status properties must be completely removed");
    }

    [Fact]
    public async Task CourseCapacity_AvailableSeats_ShouldBeUnifiedAndAccurate()
    {
        using var context = CreateInMemoryDbContext();
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var course = new Course
        {
            Id = 901,
            Code = "TEST-CAP",
            Title = "Capacity Test Course",
            MaxCapacity = 3,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Courses.Add(course);

        // 2 Active enrollments, 1 Dropped (withdrawn) enrollment
        context.Enrollments.AddRange(
            new Enrollment { CourseId = 901, StudentId = 1, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = 901, StudentId = 2, Status = EnrollmentStatus.Active },
            new Enrollment { CourseId = 901, StudentId = 3, Status = EnrollmentStatus.Dropped }
        );
        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new Application.Shaping.DataShaper<Application.DTOs.CourseDtos.CourseDto>());
        var courseDto = await service.GetCourseByIdAsync(901);

        courseDto.Should().NotBeNull();
        courseDto!.MaxCapacity.Should().Be(3);
        // Only Active/Pending count towards capacity, Dropped does not
        courseDto.EnrolledCount.Should().Be(2);
        courseDto.AvailableSeats.Should().Be(1);
        courseDto.IsFull.Should().BeFalse();
    }

    [Fact]
    public async Task FrenchForBeginners_ShouldBeActive_AndEnrollmentCalculatesAvailability()
    {
        using var context = CreateInMemoryDbContext();
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var frenchCourse = new Course
        {
            Id = 902,
            Code = "LAN-101",
            Title = "French for Beginners",
            MaxCapacity = 30,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(4),
            EndDate = DateTime.UtcNow.AddDays(40)
        };
        context.Courses.Add(frenchCourse);
        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new Application.Shaping.DataShaper<Application.DTOs.CourseDtos.CourseDto>());
        var courseDto = await service.GetCourseByIdAsync(902);

        courseDto.Should().NotBeNull();
        courseDto!.Status.Should().Be("Active");
        courseDto.EnrolledCount.Should().Be(0);
        courseDto.AvailableSeats.Should().Be(30);
        courseDto.IsFull.Should().BeFalse();
    }

    [Fact]
    public async Task StudentWithdrawal_BeforeStartDate_ShouldSucceedAndReleaseSeat()
    {
        using var context = CreateInMemoryDbContext();
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var upcomingCourse = new Course
        {
            Id = 903,
            Code = "WITHDRAW-101",
            Title = "Upcoming Course For Withdrawal",
            MaxCapacity = 2,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(35)
        };
        context.Courses.Add(upcomingCourse);

        var enrollment = new Enrollment
        {
            Id = 777,
            CourseId = 903,
            StudentId = 55,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        // Simulate withdrawal before start date
        enrollment.Status = EnrollmentStatus.Dropped;
        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new Application.Shaping.DataShaper<Application.DTOs.CourseDtos.CourseDto>());
        var courseDto = await service.GetCourseByIdAsync(903);

        courseDto!.EnrolledCount.Should().Be(0);
        courseDto.AvailableSeats.Should().Be(2);

        var myCourses = await service.GetStudentEnrolledCoursesAsync(55);
        myCourses.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleService_WithdrawnEnrollment_ShouldNotAppearOnStudentCalendar()
    {
        using var context = CreateInMemoryDbContext();
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var course = new Course
        {
            Id = 904,
            Code = "SCHED-WITHDRAW",
            Title = "Schedule Test Course",
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        course.Schedules.Add(new CourseSchedule
        {
            Id = 501,
            CourseId = 904,
            DayOfWeek = DateTime.UtcNow.AddDays(3).DayOfWeek,
            StartTime = "10:00 AM",
            EndTime = "11:00 AM"
        });
        context.Courses.Add(course);

        // Enrollment is Dropped / Withdrawn
        context.Enrollments.Add(new Enrollment
        {
            CourseId = 904,
            StudentId = 88,
            Status = EnrollmentStatus.Dropped
        });
        await context.SaveChangesAsync();

        var scheduleService = new ScheduleService(context, mapper);
        var calendar = await scheduleService.GetStudentScheduleOverviewAsync(
            88,
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month
        );

        calendar.Should().NotBeNull();
        calendar.EnrolledCoursesCount.Should().Be(0);
        calendar.Schedules.Should().BeEmpty();
    }

    [Fact]
    public void CreateEnrollmentDto_JsonSerialization_ShouldSupportStringEnumValues()
    {
        var json = """
        {
            "studentId": 10,
            "courseId": 4,
            "batch": "Batch 1",
            "status": "Active"
        }
        """;

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var dto = System.Text.Json.JsonSerializer.Deserialize<Application.DTOs.EnrollmentDtos.CreateEnrollmentDto>(json, options);

        dto.Should().NotBeNull();
        dto!.StudentId.Should().Be(10);
        dto.CourseId.Should().Be(4);
        dto.Status.Should().Be(EnrollmentStatus.Active);
    }

    [Theory]
    [InlineData(1, 0, 0.0)]
    [InlineData(1, 1, 100.0)]
    [InlineData(2, 0, 0.0)]
    [InlineData(2, 1, 50.0)]
    [InlineData(2, 2, 100.0)]
    [InlineData(5, 0, 0.0)]
    [InlineData(5, 1, 20.0)]
    [InlineData(5, 2, 40.0)]
    [InlineData(5, 3, 60.0)]
    [InlineData(5, 4, 80.0)]
    [InlineData(5, 5, 100.0)]
    [InlineData(10, 1, 10.0)]
    [InlineData(10, 5, 50.0)]
    [InlineData(10, 10, 100.0)]
    public async Task CourseProgress_BasedOnConfiguredAssignments_CalculatesAccuratePercentages(int totalAssignments, int completedCount, decimal expectedPercentage)
    {
        using var context = CreateInMemoryDbContext();
        var config = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<Application.Mappings.MappingProfile>());
        var mapper = config.CreateMapper();

        var courseId = 1500 + totalAssignments * 10 + completedCount;
        var studentId = 700 + completedCount;

        var course = new Course
        {
            Id = courseId,
            Code = $"PRG-{totalAssignments}",
            Title = $"Progress Course {totalAssignments}",
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(20),
            ExpectedAssignmentCount = totalAssignments,
            ExpectedMidtermCount = 0,
            ExpectedFinalCount = 0
        };
        context.Courses.Add(course);

        // Add configured assignments
        var assignments = new List<Assignment>();
        for (int i = 1; i <= totalAssignments; i++)
        {
            assignments.Add(new Assignment
            {
                Id = courseId * 100 + i,
                CourseId = courseId,
                Title = $"Assignment {i}",
                MaxPoints = 100m,
                DueDate = DateTime.UtcNow.AddDays(i),
                IsPublished = true
            });
        }
        context.Assignments.AddRange(assignments);

        // Add enrollment
        var enrollment = new Enrollment
        {
            Id = courseId * 10,
            CourseId = courseId,
            StudentId = studentId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddDays(-5)
        };
        context.Enrollments.Add(enrollment);

        // Add student completed assessments
        for (int i = 0; i < completedCount; i++)
        {
            context.Assessments.Add(new Assessment
            {
                Id = courseId * 1000 + i + 1,
                CourseId = courseId,
                StudentId = studentId,
                AssignmentId = assignments[i].Id,
                Title = assignments[i].Title,
                MaxScore = 100m,
                DueDate = assignments[i].DueDate,
                Status = "Graded",
                ScoredPoints = 90m,
                SubmittedAt = DateTime.UtcNow.AddHours(-i),
                GradedAt = DateTime.UtcNow.AddHours(-i)
            });
        }

        await context.SaveChangesAsync();

        var service = new CourseService(context, mapper, new Application.Shaping.DataShaper<Application.DTOs.CourseDtos.CourseDto>());
        var studentCourses = await service.GetStudentEnrolledCoursesAsync(studentId);

        studentCourses.Should().HaveCount(1);
        var enrolledCourse = studentCourses.First();
        enrolledCourse.ProgressPercentage.Should().Be(expectedPercentage);
    }

    [Fact]
    public async Task StudentService_GetProfileAsync_ShouldReturnAccurateAcademicStatistics()
    {
        using var context = CreateInMemoryDbContext();

        var user = new TmsUser
        {
            Id = "user-profile-test-1",
            UserName = "student.stats@tms.com",
            Email = "student.stats@tms.com",
            FirstName = "Alice",
            LastName = "Johnson",
            PhoneNumber = "555-1234",
            Department = "Computer Science",
            CreatedAt = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Users.Add(user);

        var student = new Student
        {
            Id = 501,
            UserId = user.Id,
            RegistrationNumber = "STU-00501",
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "student.stats@tms.com",
            EnrollmentDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Students.Add(student);

        // Add 3 courses
        var course1 = new Course { Id = 101, Code = "CS101", Title = "Intro to CS", Credits = 3, Status = "Active" };
        var course2 = new Course { Id = 102, Code = "CS102", Title = "Data Structures", Credits = 4, Status = "Active" };
        var course3 = new Course { Id = 103, Code = "CS103", Title = "Algorithms", Credits = 3, Status = "Active" };
        context.Courses.AddRange(course1, course2, course3);

        // Add Enrollments: 1 Completed with Grade 95 (4.0 GP, 3 cr), 1 Active with Grade 85 (3.0 GP, 4 cr), 1 Dropped
        context.Enrollments.AddRange(
            new Enrollment { Id = 1, StudentId = student.Id, CourseId = course1.Id, Status = EnrollmentStatus.Completed, Grade = 95m },
            new Enrollment { Id = 2, StudentId = student.Id, CourseId = course2.Id, Status = EnrollmentStatus.Active, Grade = 85m },
            new Enrollment { Id = 3, StudentId = student.Id, CourseId = course3.Id, Status = EnrollmentStatus.Dropped }
        );

        // Add 1 Certificate
        context.Certificates.Add(new Certificate
        {
            Id = 1,
            StudentId = student.Id,
            CourseId = course1.Id,
            CertificateNumber = "CERT-501-101",
            IssueDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var mapperMock = new Mock<AutoMapper.IMapper>();
        var gradingService = new Application.Grading.GradingService();
        var studentService = new StudentService(context, mapperMock.Object, null, gradingService);

        var profile = await studentService.GetProfileAsync(user.Id);

        profile.Should().NotBeNull();
        profile!.Id.Should().Be(501);
        profile.RegistrationNumber.Should().Be("STU-00501");
        profile.TotalEnrollments.Should().Be(2); // Excludes Dropped
        profile.ActiveCourses.Should().Be(1);
        profile.CompletedCourses.Should().Be(1);
        profile.EarnedCertificates.Should().Be(1);
        // GPA calculation: (4.0 * 3 + 3.0 * 4) / 7 = 24.0 / 7 = 3.43
        profile.Gpa.Should().Be(3.43m);
    }
}
