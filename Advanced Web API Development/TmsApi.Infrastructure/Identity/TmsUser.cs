using Microsoft.AspNetCore.Identity;

namespace TmsApi.Infrastructure.Identity;

public class TmsUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? StudentId { get; set; }
    public int? InstructorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
