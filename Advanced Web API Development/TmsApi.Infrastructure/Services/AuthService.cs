using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.AuthDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class AuthService(
    UserManager<TmsUser> _userManager,
    TokenService _tokenService,
    TmsDbContext _context) : IAuthService
{
    public async Task<(bool Succeeded, AuthResponseDto? Response, object? Errors)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return (false, null, new { message = "User with this email already exists." });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var roleToAssign = request.Role is "Admin" or "Instructor" or "Student" ? request.Role : "Student";

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return (false, null, result.Errors);
        }

        await _userManager.AddToRoleAsync(user, roleToAssign);

        if (string.Equals(roleToAssign, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var student = new Student
            {
                UserId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                RegistrationNumber = request.RegNumber ?? $"REG-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                EnrollmentDate = DateTime.UtcNow
            };
            await _context.Students.AddAsync(student, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            user.StudentId = student.Id;
            await _userManager.UpdateAsync(user);
        }
        else if (string.Equals(roleToAssign, "Instructor", StringComparison.OrdinalIgnoreCase))
        {
            var instructor = new Instructor
            {
                UserId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                EmployeeNumber = $"EMP-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                Department = "Computer Science",
                HireDate = DateTime.UtcNow
            };
            await _context.Instructors.AddAsync(instructor, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            user.InstructorId = instructor.Id;
            await _userManager.UpdateAsync(user);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, roles);

        var userDto = new AuthUserResponseDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentId,
            user.InstructorId,
            roles.FirstOrDefault() ?? roleToAssign,
            roles
        );

        return (true, new AuthResponseDto(userDto, accessToken, accessToken, refreshToken), null);
    }

    public async Task<(bool Succeeded, AuthResponseDto? Response, string? ErrorMessage)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return (false, null, "Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, null, "Your account is suspended. Please contact an administrator.");
        }

        if (user.InstructorId.HasValue)
        {
            var instructor = await _context.Instructors.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == user.InstructorId.Value, cancellationToken);
            if (instructor != null && string.Equals(instructor.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "Your account is suspended. Please contact an administrator.");
            }
        }
        else if (user.StudentId.HasValue)
        {
            var student = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == user.StudentId.Value, cancellationToken);
            if (student != null && string.Equals(student.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "Your account is suspended. Please contact an administrator.");
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, roles);

        var userDto = new AuthUserResponseDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentId,
            user.InstructorId,
            roles.FirstOrDefault() ?? "Student",
            roles
        );

        return (true, new AuthResponseDto(userDto, accessToken, accessToken, refreshToken), null);
    }

    public async Task<RefreshResponseDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokens = await _tokenService.RefreshTokenAsync(refreshToken);
        if (!tokens.HasValue) return null;

        return new RefreshResponseDto(tokens.Value.AccessToken, tokens.Value.AccessToken, tokens.Value.RefreshToken);
    }

    public async Task RevokeTokenAsync(string? refreshToken, string? userId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _tokenService.RevokeTokenAsync(refreshToken);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            await _tokenService.RevokeAllUserTokensAsync(userId);
        }
    }

    public async Task<AuthUserResponseDto?> GetCurrentUserProfileAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthUserResponseDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentId,
            user.InstructorId,
            roles.FirstOrDefault() ?? "Student",
            roles
        );
    }
}
