namespace proyecto_prograwebG4.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
        
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public DateTime BirthDate { get; set; } = DateTime.UtcNow;

    public string Country { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string Role { get; set; } = "user";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;
}