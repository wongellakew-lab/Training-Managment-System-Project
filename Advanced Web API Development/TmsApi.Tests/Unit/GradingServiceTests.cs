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
}
