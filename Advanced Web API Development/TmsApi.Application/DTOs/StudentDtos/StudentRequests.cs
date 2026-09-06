namespace TmsApi.Application.DTOs.StudentDtos;

public record UpdateStudentProfileRequest(string FirstName, string LastName, string? PhoneNumber, string? Department, string? Bio);
public record ChangeStudentPasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
