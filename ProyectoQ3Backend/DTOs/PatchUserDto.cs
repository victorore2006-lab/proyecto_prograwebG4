using System;

namespace ProyectoQ3Backend.DTOs;

public class PatchUserDto
{
    public string? DisplayName { get; set; }
    public string? Username { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Country { get; set; }
    public string? Bio { get; set; }
}
