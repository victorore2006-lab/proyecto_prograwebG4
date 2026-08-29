namespace ProyectoQ3Backend.DTOs;

public class AuthResponseDto
{
    public string IdToken { get; set; } = string.Empty;
    public string LocalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
