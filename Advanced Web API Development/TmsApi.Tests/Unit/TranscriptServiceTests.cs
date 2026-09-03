using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Grading;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;
using Xunit;

namespace TmsApi.Tests.Unit;

public class TranscriptServiceTests
{
    private TmsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TmsDbContext(options);
    }

    [Fact]
    public async Task GetStudentTranscriptAsync_WithMultipleCompletedCourses_CalculatesCorrectCreditsAndGPA()
    {
        using var context = CreateInMemoryDbContext();
        var gradingService = new GradingService();
        var sut = new TranscriptService(context, gradingService);

        var student = new Student
        {
            Id = 10,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@computing.org",
            RegistrationNumber = "REG-ADA-001",
            Department = "Computer Science"
        };

        var course1 = new Course { Id = 101, Code = "CS101", Title = "Intro to Programming", Credits = 3, Category = "Web Dev" };
        var course2 = new Course { Id = 102, Code = "CS201", Title = "Data Structures", Credits = 4, Category = "Computer Science" };

        var enrollment1 = new Enrollment
        {
            Id = 1,
            StudentId = student.Id,
            CourseId = course1.Id,
            Course = course1,
            Grade = 92m, // Grade A, 4.0 pts
            Status = EnrollmentStatus.Completed
        };

        var enrollment2 = new Enrollment
        {
            Id = 2,
            StudentId = student.Id,
            CourseId = course2.Id,
            Course = course2,
            Grade = 84m, // Grade B, 3.0 pts
            Status = EnrollmentStatus.Completed
        };

        context.Students.Add(student);
        context.Courses.AddRange(course1, course2);
        context.Enrollments.AddRange(enrollment1, enrollment2);
        await context.SaveChangesAsync();

        var result = await sut.GetStudentTranscriptAsync(10);

        result.Should().NotBeNull();
        result!.Student.FullName.Should().Be("Ada Lovelace");
        result.Student.RegistrationNumber.Should().Be("REG-ADA-001");
        result.Summary.TotalCoursesCompleted.Should().Be(2);
        result.Summary.TotalCreditsEarned.Should().Be(7); // 3 + 4
        result.Summary.CumulativeGpa.Should().Be(3.50m); // (4.0 + 3.0) / 2 = 3.50
        result.Summary.AcademicStanding.Should().Contain("Good Standing");

        result.CourseRecords.Should().HaveCount(2);
        result.CourseRecords[0].GradeLetter.Should().Be("A");
        result.CourseRecords[0].GradePoint.Should().Be(4.0m);
        result.CourseRecords[1].GradeLetter.Should().Be("B");
        result.CourseRecords[1].GradePoint.Should().Be(3.0m);
    }

    [Fact]
    public async Task GetStudentTranscriptAsync_WithNoCompletedCourses_ReturnsGracefulSummary()
    {
        using var context = CreateInMemoryDbContext();
        var gradingService = new GradingService();
        var sut = new TranscriptService(context, gradingService);

        var student = new Student
        {
            Id = 20,
            FirstName = "Charles",
            LastName = "Babbage",
            Email = "charles@engines.org",
            RegistrationNumber = "REG-CH-002"
        };

        context.Students.Add(student);
        await context.SaveChangesAsync();

        var result = await sut.GetStudentTranscriptAsync(20);

        result.Should().NotBeNull();
        result!.Summary.TotalCoursesCompleted.Should().Be(0);
        result.Summary.TotalCreditsEarned.Should().Be(0);
        result.Summary.CumulativeGpa.Should().Be(0.0m);
        result.CourseRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentTranscriptAsync_ExcludesRejectedEnrollments_AndMarksDroppedWithdrawn()
    {
        using var context = CreateInMemoryDbContext();
        var gradingService = new GradingService();
        var sut = new TranscriptService(context, gradingService);

        var student = new Student { Id = 30, FirstName = "Alan", LastName = "Turing", Email = "alan@enigma.org" };
        var course1 = new Course { Id = 301, Code = "MATH101", Title = "Calculus", Credits = 3 };
        var course2 = new Course { Id = 302, Code = "MATH201", Title = "Discrete Math", Credits = 3 };

        var rejected = new Enrollment
        {
            Id = 1,
            StudentId = 30,
            CourseId = 301,
            Course = course1,
            Status = EnrollmentStatus.Rejected
        };

        var dropped = new Enrollment
        {
            Id = 2,
            StudentId = 30,
            CourseId = 302,
            Course = course2,
            Status = EnrollmentStatus.Dropped
        };

        context.Students.Add(student);
        context.Courses.AddRange(course1, course2);
        context.Enrollments.AddRange(rejected, dropped);
        await context.SaveChangesAsync();

        var result = await sut.GetStudentTranscriptAsync(30);

        result.Should().NotBeNull();
        // Rejected course must not appear
        result!.CourseRecords.Should().HaveCount(1);
        result.CourseRecords[0].CourseCode.Should().Be("MATH201");
        result.CourseRecords[0].Status.Should().Be("Withdrawn");
        result.CourseRecords[0].GradeLetter.Should().Be("W");
        result.CourseRecords[0].Credits.Should().Be(0);
        result.Summary.TotalCreditsEarned.Should().Be(0);
    }

    [Fact]
    public async Task GetStudentTranscriptAsync_NonExistentStudent_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        var gradingService = new GradingService();
        var sut = new TranscriptService(context, gradingService);

        var result = await sut.GetStudentTranscriptAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateTranscriptHtmlAsync_ProducesStandardRegistrarLayout()
    {
        using var context = CreateInMemoryDbContext();
        var gradingService = new GradingService();
        var sut = new TranscriptService(context, gradingService);

        var student = new Student
        {
            Id = 40,
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@navy.mil",
            RegistrationNumber = "REG-GRACE-001"
        };
        var course = new Course { Id = 401, Code = "COMP101", Title = "Compiler Principles", Credits = 3 };
        var enrollment = new Enrollment
        {
            Id = 1,
            StudentId = 40,
            CourseId = 401,
            Course = course,
            Grade = 96m,
            Status = EnrollmentStatus.Completed
        };

        context.Students.Add(student);
        context.Courses.Add(course);
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var html = await sut.GenerateTranscriptHtmlAsync(40, "TRN-TEST-12345");

        html.Should().Contain("Official Academic Transcript");
        html.Should().Contain("Grace Hopper");
        html.Should().Contain("REG-GRACE-001");
        html.Should().Contain("COMP101");
        html.Should().Contain("Compiler Principles");
        html.Should().Contain("TRN-TEST-12345");
        html.Should().Contain("OFFICIAL INSTITUTIONAL CERTIFICATION");
    }
}
