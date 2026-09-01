using FluentAssertions;
using Moq;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using Xunit;

namespace TmsApi.Tests.Unit;

public class EnrollStudentHandlerTests
{
    private readonly Mock<IEnrollmentService> _enrollmentServiceMock = new();
    private readonly EnrollStudentHandler _handler;

    public EnrollStudentHandlerTests()
    {
        _handler = new EnrollStudentHandler(_enrollmentServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCallEnrollmentServiceAndReturnDto()
    {
        var command = new EnrollStudentCommand(1, 2);
        var expectedDto = new EnrollmentDto
        {
            Id = 10,
            StudentId = 1,
            CourseId = 2,
            Status = EnrollmentStatus.Approved
        };

        _enrollmentServiceMock.Setup(s => s.CreateEnrollmentAsync(
                It.Is<CreateEnrollmentDto>(d => d.StudentId == 1 && d.CourseId == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(10);
        result.StudentId.Should().Be(1);
        result.CourseId.Should().Be(2);

        _enrollmentServiceMock.Verify(s => s.CreateEnrollmentAsync(
            It.Is<CreateEnrollmentDto>(d => d.StudentId == 1 && d.CourseId == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
