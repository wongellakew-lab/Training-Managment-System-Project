namespace TmsApi.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
    public bool Used { get; set; }
    public bool Invalidated { get; set; }
}
