namespace proyecto_prograwebG4.DTOs;

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; } = DateTime.UtcNow;
    public string Country { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
}