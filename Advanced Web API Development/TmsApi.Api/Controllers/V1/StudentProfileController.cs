using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class StudentProfileController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly TmsDbContext _context;
    private readonly IWebHostEnvironment _env;

    public StudentProfileController(
        UserManager<TmsUser> userManager,
        TmsDbContext context,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _context = context;
        _env = env;
    }

    public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber, string? Department, string? Bio);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        Student? student = null;
        if (user.StudentId.HasValue)
        {
            student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == user.StudentId.Value);
        }
        if (student == null)
        {
            student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == user.Id);
        }

        var roles = await _userManager.GetRolesAsync(user);

        var totalEnrollments = student != null
            ? await _context.Enrollments.CountAsync(e => e.StudentId == student.Id && !e.IsDeleted)
            : 0;

        var completedCourses = student != null
            ? await _context.Enrollments.CountAsync(e => e.StudentId == student.Id && e.Status == EnrollmentStatus.Completed && !e.IsDeleted)
            : 0;

        var activeCourses = student != null
            ? await _context.Enrollments.CountAsync(e => e.StudentId == student.Id && e.Status == EnrollmentStatus.Active && !e.IsDeleted)
            : 0;

        var earnedCertificates = student != null
            ? await _context.Certificates.CountAsync(c => c.StudentId == student.Id)
            : 0;

        var regNum = !string.IsNullOrWhiteSpace(student?.RegistrationNumber)
            ? student.RegistrationNumber
            : (student != null ? $"STU-{student.Id:D5}" : $"STU-{user.Id[..Math.Min(6, user.Id.Length)].ToUpper()}");

        return Ok(new
        {
            id = user.Id,
            studentId = user.StudentId ?? student?.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            phoneNumber = user.PhoneNumber ?? student?.PhoneNumber ?? string.Empty,
            department = user.Department ?? student?.Department ?? "General Academic Program",
            bio = user.Bio ?? student?.Bio ?? string.Empty,
            profilePictureUrl = user.ProfilePictureUrl ?? student?.ProfilePictureUrl,
            registrationNumber = regNum,
            gpa = student?.Gpa ?? 0m,
            enrollmentDate = student?.EnrollmentDate ?? user.CreatedAt,
            roles,
            totalEnrollments,
            completedCourses,
            activeCourses,
            earnedCertificates
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { message = "First name and Last name are required." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.Department = request.Department?.Trim();
        user.Bio = request.Bio?.Trim();

        await _userManager.UpdateAsync(user);

        Student? student = null;
        if (user.StudentId.HasValue)
        {
            student = await _context.Students.FindAsync(user.StudentId.Value);
        }
        if (student == null)
        {
            student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        }

        if (student != null)
        {
            student.FirstName = user.FirstName;
            student.LastName = user.LastName;
            student.PhoneNumber = user.PhoneNumber;
            student.Department = user.Department;
            student.Bio = user.Bio;
            if (student.UserId == null) student.UserId = user.Id;
            if (!user.StudentId.HasValue)
            {
                user.StudentId = student.Id;
                await _userManager.UpdateAsync(user);
            }
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Profile updated successfully." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return BadRequest(new { message = "New password and confirmation password do not match." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size exceeds the 2MB limit." });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { message = "Only JPEG, PNG, and WebP images are permitted." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound(new { message = "User not found." });

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        var uniqueFileName = $"avatar_{user.Id}_{DateTime.UtcNow.Ticks}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var avatarUrl = $"/uploads/avatars/{uniqueFileName}";
        user.ProfilePictureUrl = avatarUrl;
        await _userManager.UpdateAsync(user);

        Student? student = null;
        if (user.StudentId.HasValue)
        {
            student = await _context.Students.FindAsync(user.StudentId.Value);
        }
        if (student == null)
        {
            student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        }

        if (student != null)
        {
            student.ProfilePictureUrl = avatarUrl;
            if (student.UserId == null) student.UserId = user.Id;
            await _context.SaveChangesAsync();
        }

        return Ok(new { avatarUrl, message = "Avatar uploaded successfully." });
    }
}
