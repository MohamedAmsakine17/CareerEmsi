namespace CareerEMSI.Models;

public class AuthResponse
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public User User { get; set; }
}
