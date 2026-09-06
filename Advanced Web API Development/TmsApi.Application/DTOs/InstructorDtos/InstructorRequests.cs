namespace TmsApi.Application.DTOs.InstructorDtos;

public record UpdateInstructorProfileRequest(string FirstName, string LastName, string? PhoneNumber, string? Department, string? Bio);
public record ChangeInstructorPasswordRequest(string CurrentPassword, string NewPassword);
