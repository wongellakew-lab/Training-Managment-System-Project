using FluentAssertions;
using TmsApi.Application.Grading;
using Xunit;

namespace TmsApi.Tests.Unit;

public class GradingServiceTests
{
    private readonly GradingService _sut = new();

    [Theory]
    [InlineData(95, GradeLevel.A)]
    [InlineData(85, GradeLevel.B)]
    [InlineData(75, GradeLevel.C)]
    [InlineData(65, GradeLevel.D)]
    [InlineData(50, GradeLevel.F)]
    public void CalculateGradeLevel_ShouldReturnExpectedGradeLevel(decimal score, GradeLevel expected)
    {
        var result = _sut.CalculateGradeLevel(score);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(95, 4.0)]
    [InlineData(82, 3.0)]
    [InlineData(72, 2.0)]
    [InlineData(65, 1.0)]
    [InlineData(40, 0.0)]
    public void CalculateGradePoint_ShouldReturnExpectedPoint(decimal score, decimal expected)
    {
        var result = _sut.CalculateGradePoint(score);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3.8, GradeLevel.A)]
    [InlineData(3.2, GradeLevel.B)]
    [InlineData(2.8, GradeLevel.C)]
    [InlineData(2.4, GradeLevel.D)]
    [InlineData(1.5, GradeLevel.F)]
    public void CalculateGradeLevel_WithGpaScale_ShouldReturnExpectedGradeLevel(decimal score, GradeLevel expected)
    {
        var result = _sut.CalculateGradeLevel(score);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3.8, 4.0)]
    [InlineData(3.4, 3.0)]
    [InlineData(2.9, 2.0)]
    [InlineData(2.4, 1.0)]
    [InlineData(1.5, 0.0)]
    public void CalculateGradePoint_WithGpaScale_ShouldReturnExpectedPoint(decimal score, decimal expected)
    {
        var result = _sut.CalculateGradePoint(score);
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateGPA_WithScores_ShouldReturnAverageGPA()
    {
        var scores = new List<decimal> { 95, 85, 75 }; // 4.0 + 3.0 + 2.0 = 9.0 / 3 = 3.00
        var result = _sut.CalculateGPA(scores);
        result.Should().Be(3.00m);
    }

    [Fact]
    public void CalculateGPA_EmptyScores_ShouldReturnZero()
    {
        var result = _sut.CalculateGPA(new List<decimal>());
        result.Should().Be(0.0m);
    }

    [Fact]
    public void CalculateEnrollmentGPA_WeightedByCredits_ShouldReturnAccurateGPA()
    {
        // Course 1: Score 95 (4.0 GP), 4 credits -> 16.0 points
        // Course 2: Score 85 (3.0 GP), 2 credits -> 6.0 points
        // Total points = 22.0, total credits = 6 -> 22.0 / 6 = 3.67
        var courses = new List<(decimal Grade, int Credits)>
        {
            (95m, 4),
            (85m, 2)
        };

        var result = _sut.CalculateEnrollmentGPA(courses);
        result.Should().Be(3.67m);
    }

    [Fact]
    public void CalculateEnrollmentGPA_EmptyCourses_ShouldReturnZero()
    {
        var result = _sut.CalculateEnrollmentGPA(new List<(decimal Grade, int Credits)>());
        result.Should().Be(0.0m);
    }
}
