using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly TmsDbContext _context;
    private readonly IStudentService _studentService;

    public AuthController(
        UserManager<TmsUser> userManager,
        TokenService tokenService,
        TmsDbContext context,
        IStudentService studentService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
        _studentService = studentService;
    }

    public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string Role, string? RegNumber);
    public record LoginRequest(string Email, string Password);
    public record RefreshTokenRequest(string RefreshToken);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists." });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        if (string.Equals(request.Role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            var studentDto = await _studentService.CreateStudentAsync(new CreateStudentDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                RegistrationNumber = request.RegNumber ?? $"REG-{Guid.NewGuid().ToString()[..8].ToUpper()}"
            });
            user.StudentId = studentDto.Id;
        }

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var roleToAssign = request.Role is "Admin" or "Instructor" or "Student" ? request.Role : "Student";
        await _userManager.AddToRoleAsync(user, roleToAssign);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, roles);

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.StudentId,
                user.InstructorId,
                roles
            },
            accessToken,
            refreshToken
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, roles);

        return Ok(new
        {
            user = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.StudentId,
                user.InstructorId,
                roles
            },
            accessToken,
            refreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var tokens = await _tokenService.RefreshTokenAsync(request.RefreshToken);
        if (!tokens.HasValue)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        return Ok(new
        {
            accessToken = tokens.Value.AccessToken,
            refreshToken = tokens.Value.RefreshToken
        });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentId,
            user.InstructorId,
            roles
        });
    }
}
