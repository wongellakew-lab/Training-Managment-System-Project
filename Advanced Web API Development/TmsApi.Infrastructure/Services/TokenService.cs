using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly TmsDbContext _context;

    public TokenService(IConfiguration configuration, TmsDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(TmsUser user, IList<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "super_secret_dev_key_which_is_at_least_32_bytes_long_12345");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        if (user.StudentId.HasValue)
        {
            claims.Add(new Claim("studentId", user.StudentId.Value.ToString()));
        }

        if (user.InstructorId.HasValue)
        {
            claims.Add(new Claim("instructorId", user.InstructorId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:DurationInMinutes"] ?? "60")),
            Issuer = _configuration["Jwt:Issuer"] ?? "TmsApi",
            Audience = _configuration["Jwt:Audience"] ?? "TmsClients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        var refreshToken = new RefreshToken
        {
            JwtId = token.Id,
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            CreationDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            Used = false,
            Invalidated = false
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return (accessToken, refreshToken.Token);
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string token)
    {
        var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == token);
        if (storedRefreshToken == null || storedRefreshToken.Used || storedRefreshToken.Invalidated || storedRefreshToken.ExpiryDate < DateTime.UtcNow)
        {
            return null;
        }

        storedRefreshToken.Used = true;
        _context.RefreshTokens.Update(storedRefreshToken);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(storedRefreshToken.UserId);
        if (user == null) return null;

        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
            .ToListAsync();

        return await GenerateTokensAsync(user, roles);
    }
}
